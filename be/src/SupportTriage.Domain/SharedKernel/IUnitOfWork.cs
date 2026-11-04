namespace SupportTriage.Domain.SharedKernel;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Saves and dispatches domain events; inside one transaction by default (persistence.md "DbContext and unit of work").
    Task SaveEntitiesAsync(DomainTransactionBoundary boundary = DomainTransactionBoundary.SingleTransaction, CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    void RollbackTransaction();

    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);

    void SetCommandTimeout(TimeSpan timeout);

    void ClearChangeTracker();
}
