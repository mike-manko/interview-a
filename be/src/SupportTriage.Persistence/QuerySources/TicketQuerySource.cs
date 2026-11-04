using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using SupportTriage.Application.Common.Dto;
using SupportTriage.Application.Common.Exceptions;
using SupportTriage.Application.Common.Interfaces;
using SupportTriage.Application.Tickets.Dto;
using SupportTriage.Application.Tickets.Interfaces;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Persistence.Constants;
using SupportTriage.Persistence.Documents;
using SupportTriage.Persistence.Mapping;
using SupportTriage.Persistence.Paging;
using SupportTriage.Persistence.Settings;

namespace SupportTriage.Persistence.QuerySources;

// Single-partition reads for the current tenant, projected to the fields the API returns.
internal sealed class TicketQuerySource : ITicketQuerySource
{
    private readonly Container _container;

    private readonly ITenantScope _tenantScope;

    public TicketQuerySource(CosmosClient cosmosClient, IOptions<CosmosSettings> settings, ITenantScope tenantScope)
    {
        ArgumentNullException.ThrowIfNull(cosmosClient);
        ArgumentNullException.ThrowIfNull(settings);

        _container = cosmosClient.GetContainer(settings.Value.DatabaseName, settings.Value.TicketsContainerName);
        _tenantScope = tenantScope ?? throw new ArgumentNullException(nameof(tenantScope));
    }

    // A continuation token belongs to the query that produced it: pass it back with the same filter.
    public async Task<ContinuationPageDto<TicketDto>> GetTicketsAsync(int pageSize, string? continuationToken, TicketListFilter filter, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantScope.RequireTenantId();

        string? cosmosToken = null;
        if (continuationToken is not null && !ContinuationTokenCodec.TryDecode(continuationToken, out cosmosToken))
            throw new InvalidContinuationTokenException(PersistenceErrorMessages.ContinuationTokenIsInvalid.WithKey(nameof(continuationToken)));

        var query = TicketListQueryBuilder.Build(filter);
        var options = new QueryRequestOptions { PartitionKey = new PartitionKey(tenantId), MaxItemCount = pageSize };

        using var iterator = _container.GetItemQueryIterator<TicketDocument>(query, cosmosToken, options);
        FeedResponse<TicketDocument> page;
        try
        {
            page = await iterator.ReadNextAsync(cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.BadRequest && cosmosToken is not null)
        {
            throw new InvalidContinuationTokenException(PersistenceErrorMessages.ContinuationTokenIsInvalid.WithKey(nameof(continuationToken)), ex);
        }

        var items = new List<TicketDto>(page.Count);
        foreach (var document in page)
            items.Add(TicketDocumentMapper.ToDto(document));

        return new ContinuationPageDto<TicketDto>(items, ContinuationTokenCodec.Encode(page.ContinuationToken));
    }

    public async Task<TicketDto?> GetTicketAsync(string ticketId, bool includeHidden, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticketId);

        var tenantId = _tenantScope.RequireTenantId();
        try
        {
            var response = await _container.ReadItemAsync<TicketDocument>(ticketId, new PartitionKey(tenantId), cancellationToken: cancellationToken);
            var document = response.Resource;

            if (document.IsDeleted || (document.Status == TicketStatus.Failed && !includeHidden))
                return null;

            return TicketDocumentMapper.ToDto(document);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
