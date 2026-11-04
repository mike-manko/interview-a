using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupportTriage.Application.Tickets.ApplicationServices;
using SupportTriage.Application.Tickets.Enums;
using SupportTriage.Application.Tickets.Logging;
using SupportTriage.Application.Tickets.Settings;
using SupportTriage.Domain.SharedKernel;
using SupportTriage.Domain.TicketAggregate;
using SupportTriage.Domain.TicketAggregate.Interfaces.Repositories;

namespace SupportTriage.Application.Tickets.Commands;

// Claims the ticket, runs the triage steps that are not done yet (screening for prompt injection, then
// classification) and completes it. A ticket flagged by screening is hidden and never reaches the classifier. The claim is a lease written with
// the ticket's ETag, so exactly one worker wins it; every later save also carries the ETag, so a worker whose lease
// expired cannot overwrite the new owner's work. Each step is saved as soon as it is done (a checkpoint), so a crash
// never repeats a finished step. Failures propagate to the worker, which records them with RecordTriageFailureCommand.
public sealed record TriageTicketCommand(string TicketId, string WorkerId) : IRequest<TriageTicketOutcome>;

public sealed class TriageTicketCommandHandler : IRequestHandler<TriageTicketCommand, TriageTicketOutcome>
{
    private const string ClaimStage = "claim";

    private const string ScreeningStage = "screening";

    private const string ClassificationStage = "classification";

    private const string CompletionStage = "completion";

    private readonly ITicketRepository _ticketRepository;

    private readonly ITicketScreeningStep _screeningStep;

    private readonly ITicketClassificationStep _classificationStep;

    private readonly IDateTime _dateTime;

    private readonly TicketTriageSettings _settings;

    private readonly ILogger<TriageTicketCommandHandler> _logger;

    public TriageTicketCommandHandler(ITicketRepository ticketRepository, ITicketScreeningStep screeningStep, ITicketClassificationStep classificationStep, IDateTime dateTime, IOptions<TicketTriageSettings> settings, ILogger<TriageTicketCommandHandler> logger)
    {
        _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
        _screeningStep = screeningStep ?? throw new ArgumentNullException(nameof(screeningStep));
        _classificationStep = classificationStep ?? throw new ArgumentNullException(nameof(classificationStep));
        _dateTime = dateTime ?? throw new ArgumentNullException(nameof(dateTime));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<TriageTicketOutcome> Handle(TriageTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.FindAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return TriageTicketOutcome.NotFound;

        if (!ticket.TryClaim(request.WorkerId, _settings.LeaseDuration, _dateTime))
        {
            TicketTriageLog.TicketNotClaimable(_logger, ticket.Id, ticket.TenantId, request.WorkerId);
            return TriageTicketOutcome.NotClaimable;
        }

        if (!await SaveAsync(ticket, request.WorkerId, ClaimStage, cancellationToken))
            return TriageTicketOutcome.ClaimLost;

        var screening = await _screeningStep.ExecuteAsync(ticket, cancellationToken);
        if ((screening is ScreenTicketOutcome.Safe or ScreenTicketOutcome.PromptInjection) && !await SaveAsync(ticket, request.WorkerId, ScreeningStage, cancellationToken))
            return TriageTicketOutcome.LeaseLost;

        if (ticket.IsHidden)
            return TriageTicketOutcome.FlaggedAsPromptInjection;

        var classification = await _classificationStep.ExecuteAsync(ticket, cancellationToken);
        if ((classification is ClassifyTicketOutcome.Classified or ClassifyTicketOutcome.ClassifiedByRule) && !await SaveAsync(ticket, request.WorkerId, ClassificationStage, cancellationToken))
            return TriageTicketOutcome.LeaseLost;

        ticket.CompleteTriage(request.WorkerId, _dateTime);
        if (!await SaveAsync(ticket, request.WorkerId, CompletionStage, cancellationToken))
            return TriageTicketOutcome.LeaseLost;

        TicketTriageLog.TriageCompleted(_logger, ticket.Id, ticket.TenantId);

        return TriageTicketOutcome.Completed;
    }

    private async Task<bool> SaveAsync(Ticket ticket, string workerId, string stage, CancellationToken cancellationToken)
    {
        if (await _ticketRepository.TryReplaceAsync(ticket, cancellationToken))
            return true;

        TicketTriageLog.TicketLostToAnotherWriter(_logger, workerId, ticket.Id, ticket.TenantId, stage);

        return false;
    }
}
