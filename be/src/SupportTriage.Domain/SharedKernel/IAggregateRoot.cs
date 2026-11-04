namespace SupportTriage.Domain.SharedKernel;

public interface IAggregateRoot<TId>
    where TId : notnull
{
    TId Id { get; }
}
