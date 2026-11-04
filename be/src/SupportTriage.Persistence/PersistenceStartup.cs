using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SupportTriage.Persistence.Settings;

namespace SupportTriage.Persistence;

public static class PersistenceStartup
{
    private const string TenantPartitionKeyPath = "/tenantId";

    // Long text that is never filtered or sorted on stays out of the index, which lowers the cost of every write.
    private static readonly string[] UnindexedPaths =
    [
        "/body/?",
        "/subject/?",
        "/customerEmail/?",
        "/suggestedReply/?",
        "/triageFailureNote/?",
        "/classificationSource/?",
        "/screeningSource/?",
        "/leaseOwner/?",
    ];

    // Creates the database and the tickets container once at startup, never per request (cosmos.md).
    public static async Task InitializePersistenceAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var cosmosClient = services.GetRequiredService<CosmosClient>();
        var settings = services.GetRequiredService<IOptions<CosmosSettings>>().Value;

        var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(settings.DatabaseName, cancellationToken: cancellationToken);

        var tickets = new ContainerProperties(settings.TicketsContainerName, TenantPartitionKeyPath);
        tickets.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/*" });
        foreach (var path in UnindexedPaths)
            tickets.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = path });

        // Both queries sort on a single field (createdAt, nextAttemptAt), which the default range index serves, so no
        // composite index is needed.
        await database.Database.CreateContainerIfNotExistsAsync(tickets, cancellationToken: cancellationToken);
    }
}
