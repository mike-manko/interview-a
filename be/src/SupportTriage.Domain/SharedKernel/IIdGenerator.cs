namespace SupportTriage.Domain.SharedKernel;

// Id generators may be asynchronous (a database sequence), so aggregate methods that create children may be async too.
public interface IIdGenerator<TId>
    where TId : notnull
{
    TId NewId();

    ValueTask<TId> NewIdAsync(CancellationToken cancellationToken = default);
}
