namespace SupportTriage.Domain.SharedKernel;

public interface ITenantEntityRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : TimestampedTenantEntityBase<TKey>, IAggregateRoot<TKey>
    where TKey : notnull
{
    void SoftRemove(TEntity entity, IDateTime dateTime);
}
