namespace SupportTriage.Domain.SharedKernel;

public enum DomainTransactionBoundary
{
    // Domain events are dispatched before SaveChanges, so handler changes commit with the originating change.
    SingleTransaction = 0,

    // Domain events are dispatched after SaveChanges; handlers must be idempotent and tolerate partial failure.
    MultipleTransactions = 1,
}
