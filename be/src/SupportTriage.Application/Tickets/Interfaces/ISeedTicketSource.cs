using SupportTriage.Application.Tickets.Dto;

namespace SupportTriage.Application.Tickets.Interfaces;

public interface ISeedTicketSource
{
    Task<IReadOnlyList<SeedTicketDto>> ReadSeedTicketsAsync(CancellationToken cancellationToken = default);
}
