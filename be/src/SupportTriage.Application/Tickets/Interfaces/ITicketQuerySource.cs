using SupportTriage.Application.Common.Dto;
using SupportTriage.Application.Tickets.Dto;

namespace SupportTriage.Application.Tickets.Interfaces;

// Read side, limited to the current tenant. Removed tickets are never returned; hidden tickets (failed triage) only
// when the filter or includeHidden asks for them.
public interface ITicketQuerySource
{
    Task<ContinuationPageDto<TicketDto>> GetTicketsAsync(int pageSize, string? continuationToken, TicketListFilter filter, CancellationToken cancellationToken = default);

    Task<TicketDto?> GetTicketAsync(string ticketId, bool includeHidden, CancellationToken cancellationToken = default);
}
