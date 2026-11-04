namespace SupportTriage.Domain.SharedKernel;

public abstract class EntityBase<TId> : IDomainEventsCarrier, IEquatable<EntityBase<TId>>
    where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    private readonly List<IDomainEvent> _domainEvents = new();

    private int? _cachedHashCode;

    protected EntityBase()
    {
    }

    IReadOnlyCollection<IDomainEvent> IDomainEventsCarrier.DomainEvents => _domainEvents;

    public bool IsTransient() => EqualityComparer<TId>.Default.Equals(Id, default!);

    public bool Equals(EntityBase<TId>? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType() || IsTransient() || other.IsTransient())
            return false;

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => Equals(obj as EntityBase<TId>);

    public override int GetHashCode()
    {
        if (IsTransient())
            return base.GetHashCode();

        _cachedHashCode ??= HashCode.Combine(GetType(), Id);

        return _cachedHashCode.Value;
    }

    void IDomainEventsCarrier.ClearDomainEvents() => _domainEvents.Clear();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        _domainEvents.Add(domainEvent);
    }
}
