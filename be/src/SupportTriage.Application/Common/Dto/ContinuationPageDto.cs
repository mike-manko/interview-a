namespace SupportTriage.Application.Common.Dto;

// One page of a Cosmos DB query. ContinuationToken is opaque to clients; pass it back to get the next page. It is
// null on the last page.
public sealed record ContinuationPageDto<T>(IReadOnlyList<T> Items, string? ContinuationToken);
