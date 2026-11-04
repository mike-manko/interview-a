using Microsoft.Extensions.DependencyInjection;
using SupportTriage.Domain.SharedKernel;
using SupportTriage.Domain.TicketAggregate.Validation;

namespace SupportTriage.Domain;

public static class DomainServiceCollectionExtensions
{
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IDateTime, SystemDateTime>();
        services.AddSingleton<ITicketDataLoadValidator, TicketDataLoadValidator>();

        return services;
    }
}
