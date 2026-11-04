namespace SupportTriage.Domain.SharedKernel;

// Repositories return materialized results, never IQueryable (repositories.md).
public interface IRepository<TEntity, TKey>
    where TEntity : class, IAggregateRoot<TKey>
    where TKey : notnull
{
    IUnitOfWork UnitOfWork { get; }

    ValueTask<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default);

    ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Remove(TEntity entity);

    IRepository<TEntity, TKey> WithTracking();

    IRepository<TEntity, TKey> WithNoTracking();

    IRepository<TEntity, TKey> WithNoTrackingWithIdentityResolution();

    void Detach(TEntity entity);

    ValueTask ReloadAsync(TEntity entity, CancellationToken cancellationToken = default);
}
