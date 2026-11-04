using System.Globalization;
using Mediator;
using Microsoft.Extensions.Options;
using SupportTriage.Api.Logging;
using SupportTriage.Application.Common.Interfaces;
using SupportTriage.Application.Tickets.Commands;
using SupportTriage.Application.Tickets.Dto;
using SupportTriage.Application.Tickets.Exceptions;
using SupportTriage.Application.Tickets.Queries;
using SupportTriage.Application.Tickets.Settings;
using SupportTriage.Domain.TicketAggregate.Enums;

namespace SupportTriage.Api.Workers;

// Polls for due tickets and triages them a few at a time. This is the boundary for triage failures: it catches them
// and records them through a command. Every ticket gets its own DI scope with that ticket's tenant assigned, so the
// repositories it uses can only reach that tenant's partition.
internal sealed class TicketTriageWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly TicketTriageSettings _settings;

    private readonly ILogger<TicketTriageWorker> _logger;

    private readonly string _workerId;

    public TicketTriageWorker(IServiceScopeFactory scopeFactory, IOptions<TicketTriageSettings> settings, ILogger<TicketTriageWorker> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workerId = string.Create(CultureInfo.InvariantCulture, $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            ApiLog.TriageWorkerDisabled(_logger, _workerId);
            return;
        }

        ApiLog.TriageWorkerStarted(_logger, _workerId, _settings.PollInterval, _settings.BatchSize, _settings.MaxDegreeOfParallelism);

        using var timer = new PeriodicTimer(_settings.PollInterval);
        try
        {
            do
            {
                try
                {
                    await PollAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    ApiLog.TriagePollFailed(_logger, ex, _workerId);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<TicketWorkItemDto> dueTickets;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            dueTickets = await mediator.Send(new GetDueTicketsQuery(_settings.BatchSize), cancellationToken);
        }

        if (dueTickets.Count == 0)
            return;

        var options = new ParallelOptions { MaxDegreeOfParallelism = _settings.MaxDegreeOfParallelism, CancellationToken = cancellationToken };
        await Parallel.ForEachAsync(dueTickets, options, TriageAsync);
    }

    private async ValueTask TriageAsync(TicketWorkItemDto item, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        try
        {
            scope.ServiceProvider.GetRequiredService<ITenantScope>().AssignScope(item.TenantId);

            var outcome = await mediator.Send(new TriageTicketCommand(item.TicketId, _workerId), cancellationToken);
            ApiLog.TicketTriaged(_logger, item.TicketId, item.TenantId, outcome.ToString());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TriageProviderConfigurationException ex)
        {
            ApiLog.TicketTriageFailed(_logger, ex, item.TicketId, item.TenantId);
            await RecordFailureAsync(mediator, item, TriageFailureReason.ProviderConfiguration, ex.Message, countsAsAttempt: false, cancellationToken);
        }
        catch (TriageProviderUnavailableException ex)
        {
            ApiLog.TicketTriageFailed(_logger, ex, item.TicketId, item.TenantId);
            await RecordFailureAsync(mediator, item, TriageFailureReason.ProviderUnavailable, ex.Message, countsAsAttempt: true, cancellationToken);
        }
        catch (InvalidTriageResponseException ex)
        {
            ApiLog.TicketTriageFailed(_logger, ex, item.TicketId, item.TenantId);
            await RecordFailureAsync(mediator, item, TriageFailureReason.InvalidResponse, ex.Message, countsAsAttempt: true, cancellationToken);
        }
        catch (Exception ex)
        {
            ApiLog.TicketTriageFailed(_logger, ex, item.TicketId, item.TenantId);
            await RecordFailureAsync(mediator, item, TriageFailureReason.Unexpected, ex.Message, countsAsAttempt: true, cancellationToken);
        }
    }

    private async Task RecordFailureAsync(IMediator mediator, TicketWorkItemDto item, TriageFailureReason reason, string note, bool countsAsAttempt, CancellationToken cancellationToken)
    {
        try
        {
            await mediator.Send(new RecordTriageFailureCommand(item.TicketId, _workerId, reason, note, countsAsAttempt), cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            ApiLog.TriageFailureNotRecorded(_logger, ex, item.TicketId, item.TenantId);
        }
    }
}
