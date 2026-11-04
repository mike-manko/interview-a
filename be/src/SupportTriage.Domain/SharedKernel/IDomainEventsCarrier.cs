namespace SupportTriage.Domain.SharedKernel;

// Implemented explicitly by EntityBase, so only the unit of work reads and clears the events.
public interface IDomainEventsCarrier
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
