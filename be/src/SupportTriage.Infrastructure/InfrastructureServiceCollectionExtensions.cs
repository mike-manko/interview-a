using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using SupportTriage.Application.Tickets.Interfaces;
using SupportTriage.Infrastructure.OpenAi;
using SupportTriage.Infrastructure.OpenAi.Classification;
using SupportTriage.Infrastructure.OpenAi.Screening;
using SupportTriage.Infrastructure.OpenAi.Settings;
using SupportTriage.Infrastructure.SeedData;

namespace SupportTriage.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<OpenAiSettings>()
            .Bind(configuration.GetSection(OpenAiSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        AddOpenAiResilience(services.AddHttpClient<ITicketClassifier, OpenAiTicketClassifier>(OpenAiHttpClient.ClassificationName, static client => client.Timeout = Timeout.InfiniteTimeSpan));
        AddOpenAiResilience(services.AddHttpClient<IPromptInjectionScreener, OpenAiPromptInjectionScreener>(OpenAiHttpClient.ScreeningName, static client => client.Timeout = Timeout.InfiniteTimeSpan));

        services.AddSingleton<ISeedTicketSource, SeedTicketFileSource>();

        return services;
    }

    // The resilience pipeline owns the timeouts, so HttpClient's own 100-second timeout is switched off.
    // Order: total timeout, then retry (exponential backoff with jitter, honours Retry-After), then a per-attempt timeout.
    private static void AddOpenAiResilience(IHttpClientBuilder builder)
    {
        builder.AddResilienceHandler(OpenAiHttpClient.ResiliencePipelineName, static (pipeline, context) =>
        {
            var settings = context.ServiceProvider.GetRequiredService<IOptions<OpenAiSettings>>().Value;

            pipeline
                .AddTimeout(settings.TotalTimeout)
                .AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = settings.MaxRetryAttempts,
                    Delay = settings.RetryBaseDelay,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                })
                .AddTimeout(settings.AttemptTimeout);
        });
    }
}
