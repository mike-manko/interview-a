namespace SupportTriage.Domain.SharedKernel;

// Returned by behaviors that may or may not change a child, so the caller knows whether anything happened.
public sealed record ChildUpdateResult<T>(T Child, bool IsUpdated);
