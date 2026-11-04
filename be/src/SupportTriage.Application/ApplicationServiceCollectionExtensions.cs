using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SupportTriage.Application.Common.Interfaces;
using SupportTriage.Application.Common.Tenancy;
using SupportTriage.Application.Tickets.ApplicationServices;
using SupportTriage.Application.Tickets.Settings;

namespace SupportTriage.Application;

// Handlers are registered by Mediator's source generator in the host (solution.md).
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<TicketTriageSettings>()
            .Bind(configuration.GetSection(TicketTriageSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SeedDataSettings>()
            .Bind(configuration.GetSection(SeedDataSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<ITenantScope, TenantScope>();
        services.AddScoped<ITicketScreeningStep, TicketScreeningStep>();
        services.AddScoped<ITicketClassificationStep, TicketClassificationStep>();

        return services;
    }
}
