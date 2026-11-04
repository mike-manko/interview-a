namespace SupportTriage.Domain.TicketAggregate.Interfaces.Repositories;

// Shaped for Cosmos DB: there is no change tracker or transaction, so each write saves one ticket and is conditional
// on the version the ticket was loaded with. The implementation limits every call to the current tenant's partition.
public interface ITicketRepository
{
    ValueTask<Ticket?> FindAsync(string ticketId, CancellationToken cancellationToken = default);

    // Returns false when a ticket with the same id already exists in the tenant.
    Task<bool> TryAddAsync(Ticket ticket, CancellationToken cancellationToken = default);

    // Returns false when the ticket changed (or disappeared) since it was loaded; the caller decides what that means.
    Task<bool> TryReplaceAsync(Ticket ticket, CancellationToken cancellationToken = default);
}
