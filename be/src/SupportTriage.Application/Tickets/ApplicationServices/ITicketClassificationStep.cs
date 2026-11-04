using SupportTriage.Application.Tickets.Enums;
using SupportTriage.Domain.TicketAggregate;

namespace SupportTriage.Application.Tickets.ApplicationServices;

// The classification step of triage (category and priority in one call). Changes the ticket in memory only; the
// caller saves it when the outcome is Classified or ClassifiedByRule.
public interface ITicketClassificationStep
{
    ValueTask<ClassifyTicketOutcome> ExecuteAsync(Ticket ticket, CancellationToken cancellationToken = default);
}
