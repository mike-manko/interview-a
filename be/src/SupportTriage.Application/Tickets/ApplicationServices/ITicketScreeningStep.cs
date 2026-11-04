using SupportTriage.Application.Tickets.Enums;
using SupportTriage.Domain.TicketAggregate;

namespace SupportTriage.Application.Tickets.ApplicationServices;

// The screening step of triage, run before classification. Changes the ticket in memory only; the caller saves it
// when the outcome is Safe or PromptInjection.
public interface ITicketScreeningStep
{
    ValueTask<ScreenTicketOutcome> ExecuteAsync(Ticket ticket, CancellationToken cancellationToken = default);
}
