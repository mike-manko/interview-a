using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using SupportTriage.Application.Common.Interfaces;
using SupportTriage.Domain.TicketAggregate;
using SupportTriage.Domain.TicketAggregate.Interfaces.Repositories;
using SupportTriage.Persistence.Documents;
using SupportTriage.Persistence.Mapping;
using SupportTriage.Persistence.Settings;

namespace SupportTriage.Persistence.Repositories;

// Every operation addresses exactly one partition: the tenant of the current scope. Reads are point reads (id plus
// partition key). Writes carry the ETag the ticket was loaded with and skip the response body (the new ETag comes
// back in the headers), which keeps the cost per write down.
internal sealed class TicketRepository : ITicketRepository
{
    private static readonly ItemRequestOptions CreateOptions = new() { EnableContentResponseOnWrite = false };

    private readonly Container _container;

    private readonly ITenantScope _tenantScope;

    public TicketRepository(CosmosClient cosmosClient, IOptions<CosmosSettings> settings, ITenantScope tenantScope)
    {
        ArgumentNullException.ThrowIfNull(cosmosClient);
        ArgumentNullException.ThrowIfNull(settings);

        _container = cosmosClient.GetContainer(settings.Value.DatabaseName, settings.Value.TicketsContainerName);
        _tenantScope = tenantScope ?? throw new ArgumentNullException(nameof(tenantScope));
    }

    public async ValueTask<Ticket?> FindAsync(string ticketId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticketId);

        var tenantId = _tenantScope.RequireTenantId();
        try
        {
            var response = await _container.ReadItemAsync<TicketDocument>(ticketId, new PartitionKey(tenantId), cancellationToken: cancellationToken);

            return TicketDocumentMapper.ToTicket(response.Resource, response.ETag);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> TryAddAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        _tenantScope.EnsureCanAccessTenant(ticket.TenantId);
        try
        {
            var response = await _container.CreateItemAsync(TicketDocumentMapper.ToDocument(ticket), new PartitionKey(ticket.TenantId), CreateOptions, cancellationToken);
            ticket.AcceptVersion(response.ETag);

            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            return false;
        }
    }

    public async Task<bool> TryReplaceAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        if (ticket.Version is null)
            throw new ArgumentException("Only a ticket that was loaded from or saved to the store can be replaced.", nameof(ticket));

        _tenantScope.EnsureCanAccessTenant(ticket.TenantId);
        var options = new ItemRequestOptions { IfMatchEtag = ticket.Version, EnableContentResponseOnWrite = false };
        try
        {
            var response = await _container.ReplaceItemAsync(TicketDocumentMapper.ToDocument(ticket), ticket.Id, new PartitionKey(ticket.TenantId), options, cancellationToken);
            ticket.AcceptVersion(response.ETag);

            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode is HttpStatusCode.PreconditionFailed or HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
