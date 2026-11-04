using SupportTriage.Application.Tickets.Dto;

namespace SupportTriage.Application.Tickets.Interfaces;

// The one cross-tenant read: finds tickets that are due for triage in every tenant. It returns only ids, and the
// worker processes each ticket inside that ticket's own tenant scope. A queue can replace it later without changing
// how tickets are claimed and processed.
public interface ITicketWorkSource
{
    Task<IReadOnlyList<TicketWorkItemDto>> GetDueTicketsAsync(int maxCount, DateTime now, CancellationToken cancellationToken = default);
}
