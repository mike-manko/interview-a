using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using SupportTriage.Application.Tickets.Dto;
using SupportTriage.Application.Tickets.Interfaces;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Persistence.Documents;
using SupportTriage.Persistence.Serialization;
using SupportTriage.Persistence.Settings;

namespace SupportTriage.Persistence.QuerySources;

// The one deliberate cross-partition query: it finds due tickets in every tenant and returns only their ids, oldest
// first. At hundreds of tickets a day this is cheap; a queue fed by the change feed can replace it later.
internal sealed class TicketWorkSource : ITicketWorkSource
{
    private const string DueQuery =
        "SELECT c.id, c.tenantId FROM c WHERE c.isDeleted = false AND " +
        "((c.status = @pending AND c.nextAttemptAt <= @now) OR (c.status = @processing AND c.leaseExpiresAt <= @now)) " +
        "ORDER BY c.nextAttemptAt";

    private readonly Container _container;

    public TicketWorkSource(CosmosClient cosmosClient, IOptions<CosmosSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(cosmosClient);
        ArgumentNullException.ThrowIfNull(settings);

        _container = cosmosClient.GetContainer(settings.Value.DatabaseName, settings.Value.TicketsContainerName);
    }

    public async Task<IReadOnlyList<TicketWorkItemDto>> GetDueTicketsAsync(int maxCount, DateTime now, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCount, 1);

        var query = new QueryDefinition(DueQuery)
            .WithParameter("@pending", nameof(TicketStatus.Pending))
            .WithParameter("@processing", nameof(TicketStatus.Processing))
            .WithParameter("@now", UtcDateTimeConverter.ToQueryValue(now));

        var items = new List<TicketWorkItemDto>(maxCount);
        using var iterator = _container.GetItemQueryIterator<TicketWorkItemDocument>(query, requestOptions: new QueryRequestOptions { MaxItemCount = maxCount });

        // Cross-partition pages can be empty while more results follow, so keep reading until the batch is full.
        while (iterator.HasMoreResults && items.Count < maxCount)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            foreach (var document in page)
            {
                if (items.Count == maxCount)
                    break;

                items.Add(new TicketWorkItemDto(document.Id, document.TenantId));
            }
        }

        return items;
    }
}
