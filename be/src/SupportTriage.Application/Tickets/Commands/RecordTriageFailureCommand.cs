using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupportTriage.Application.Tickets.Logging;
using SupportTriage.Application.Tickets.Policies;
using SupportTriage.Application.Tickets.Settings;
using SupportTriage.Domain.SharedKernel;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.Interfaces.Repositories;

namespace SupportTriage.Application.Tickets.Commands;

// Sent by the worker when triage threw. Only the worker that still holds the lease can record the failure, so a
// failure reported twice, or by a worker that already lost the ticket, changes nothing. CountsAsAttempt is false for
// failures that are not the ticket's fault (a rejected API key), which postpone triage without bringing the ticket
// closer to being hidden. Reason is shown to operators as a fixed message; Note is the internal technical detail.
// Returns true when the failure was saved.
public sealed record RecordTriageFailureCommand(string TicketId, string WorkerId, TriageFailureReason Reason, string Note, bool CountsAsAttempt) : IRequest<bool>;

public sealed class RecordTriageFailureCommandHandler : IRequestHandler<RecordTriageFailureCommand, bool>
{
    private readonly ITicketRepository _ticketRepository;

    private readonly IDateTime _dateTime;

    private readonly TicketTriageSettings _settings;

    private readonly ILogger<RecordTriageFailureCommandHandler> _logger;

    public RecordTriageFailureCommandHandler(ITicketRepository ticketRepository, IDateTime dateTime, IOptions<TicketTriageSettings> settings, ILogger<RecordTriageFailureCommandHandler> logger)
    {
        _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
        _dateTime = dateTime ?? throw new ArgumentNullException(nameof(dateTime));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<bool> Handle(RecordTriageFailureCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.FindAsync(request.TicketId, cancellationToken);
        if (ticket is null || !ticket.IsLeasedBy(request.WorkerId, _dateTime))
            return false;

        if (request.CountsAsAttempt)
        {
            var retryDelay = TriageRetryPolicy.DelayFor(ticket.FailedTriageAttempts + 1, _settings.RetryBaseDelay, _settings.MaxRetryDelay);
            ticket.RecordTriageFailure(request.Reason, request.Note, _settings.MaxAttempts, retryDelay, _dateTime);
        }
        else
        {
            ticket.DeferTriage(request.Reason, request.Note, _settings.ConfigurationErrorRetryDelay, _dateTime);
        }

        if (!await _ticketRepository.TryReplaceAsync(ticket, cancellationToken))
            return false;

        if (ticket.IsHidden)
            TicketTriageLog.TicketHiddenAfterFailedTriage(_logger, ticket.Id, ticket.TenantId, ticket.FailedTriageAttempts, request.Note);
        else if (request.CountsAsAttempt)
            TicketTriageLog.TriageAttemptFailed(_logger, ticket.FailedTriageAttempts, _settings.MaxAttempts, ticket.Id, ticket.TenantId, ticket.NextAttemptAt, request.Note);
        else
            TicketTriageLog.TriageDeferred(_logger, ticket.Id, ticket.TenantId, ticket.NextAttemptAt, request.Note);

        return true;
    }
}
