using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SupportTriage.Application.Tickets.Interfaces;
using SupportTriage.Domain.SharedKernel;
using SupportTriage.Domain.TicketAggregate.Interfaces.Repositories;
using SupportTriage.Persistence.IdGenerators;
using SupportTriage.Persistence.QuerySources;
using SupportTriage.Persistence.Repositories;
using SupportTriage.Persistence.Settings;

namespace SupportTriage.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<CosmosSettings>()
            .Bind(configuration.GetSection(CosmosSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // One client for the whole process, in Direct mode (the SDK default); it retries 429 responses itself.
        services.AddSingleton(static serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<CosmosSettings>>().Value;
            var options = new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                },
            };

            return new CosmosClient(settings.Endpoint.ToString(), settings.Key, options);
        });

        services.AddSingleton<IIdGenerator<string>, TicketIdGenerator>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ITicketQuerySource, TicketQuerySource>();
        services.AddScoped<ITicketWorkSource, TicketWorkSource>();

        return services;
    }
}
