using Microsoft.Extensions.Logging;
using SupportTriage.Application.Tickets.Enums;
using SupportTriage.Application.Tickets.Interfaces;
using SupportTriage.Application.Tickets.Logging;
using SupportTriage.Domain.SharedKernel;
using SupportTriage.Domain.TicketAggregate;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.Services;
using SupportTriage.Domain.TicketAggregate.ValueObjects;

namespace SupportTriage.Application.Tickets.ApplicationServices;

// Layered prompt-injection screening: the deterministic rules run first and, when one matches, the ticket is flagged
// without any model call. Otherwise the dedicated screening model judges the ticket. Safe to run again: a screened,
// hidden, removed or completed ticket is left alone.
internal sealed class TicketScreeningStep : ITicketScreeningStep
{
    private readonly IPromptInjectionScreener _screener;

    private readonly IDateTime _dateTime;

    private readonly ILogger<TicketScreeningStep> _logger;

    public TicketScreeningStep(IPromptInjectionScreener screener, IDateTime dateTime, ILogger<TicketScreeningStep> logger)
    {
        _screener = screener ?? throw new ArgumentNullException(nameof(screener));
        _dateTime = dateTime ?? throw new ArgumentNullException(nameof(dateTime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<ScreenTicketOutcome> ExecuteAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        if (ticket.IsDeleted || ticket.IsHidden || ticket.IsCompleted)
            return ScreenTicketOutcome.NotApplicable;

        if (ticket.IsScreened)
            return ScreenTicketOutcome.AlreadyScreened;

        TicketScreening screening;
        if (!ticket.HasContent)
        {
            screening = TicketScreening.ForEmptyTicket;
        }
        else if (PromptInjectionRules.TryMatch(ticket.Subject, ticket.Body, out var ruleName))
        {
            screening = TicketScreening.FlaggedByRule(ruleName);
        }
        else
        {
            var result = await _screener.ScreenAsync(ticket.Subject, ticket.Body, cancellationToken);
            screening = new TicketScreening(result.IsPromptInjection ? ScreeningVerdict.PromptInjection : ScreeningVerdict.Safe, result.Source, null);
        }

        if (!ticket.RecordScreening(screening, _dateTime))
            return ScreenTicketOutcome.AlreadyScreened;

        if (screening.Verdict == ScreeningVerdict.PromptInjection)
        {
            TicketTriageLog.PromptInjectionDetected(_logger, ticket.Id, ticket.TenantId, screening.Source);
            return ScreenTicketOutcome.PromptInjection;
        }

        TicketTriageLog.TicketScreenedSafe(_logger, ticket.Id, ticket.TenantId, screening.Source);

        return ScreenTicketOutcome.Safe;
    }
}
