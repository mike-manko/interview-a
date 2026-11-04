using System.Text.Json.Serialization;
using Mediator;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using SupportTriage.Api.Filters;
using SupportTriage.Api.Logging;
using SupportTriage.Api.Middleware;
using SupportTriage.Api.OpenApi;
using SupportTriage.Api.Workers;
using SupportTriage.Application;
using SupportTriage.Application.Tickets.Commands;
using SupportTriage.Application.Tickets.Settings;
using SupportTriage.Domain;
using SupportTriage.Infrastructure;
using SupportTriage.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers(static options => options.Filters.Add<ApiExceptionFilter>())
    .AddJsonOptions(static options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi("v1", static options => options.AddOperationTransformer<TenantHeaderOperationTransformer>());
builder.Services.AddMediator(static (MediatorOptions options) => options.ServiceLifetime = ServiceLifetime.Scoped);
builder.Services
    .AddDomain()
    .AddApplication(builder.Configuration)
    .AddPersistence(builder.Configuration)
    .AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<TicketTriageWorker>();

var app = builder.Build();

await app.Services.InitializePersistenceAsync(app.Lifetime.ApplicationStopping);
await ImportSeedTicketsAsync(app.Services, app.Lifetime.ApplicationStopping);

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<TenantMiddleware>();

app.MapOpenApi();
app.MapScalarApiReference(static options => options.WithTitle("Support Triage API"));
app.MapControllers();

await app.RunAsync();

// Runs before the triage worker starts, in its own scope because the import uses an unrestricted tenant scope.
static async Task ImportSeedTicketsAsync(IServiceProvider services, CancellationToken cancellationToken)
{
    if (!services.GetRequiredService<IOptions<SeedDataSettings>>().Value.ImportOnStartup)
    {
        ApiLog.SeedImportDisabled(services.GetRequiredService<ILoggerFactory>().CreateLogger("SupportTriage.Api.Startup"));
        return;
    }

    await using var scope = services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new ImportSeedTicketsCommand(), cancellationToken);
}
