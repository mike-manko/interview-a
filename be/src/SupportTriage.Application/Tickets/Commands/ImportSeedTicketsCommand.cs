using Mediator;
using Microsoft.Extensions.Logging;
using SupportTriage.Application.Common.Interfaces;
using SupportTriage.Application.Tickets.Interfaces;
using SupportTriage.Application.Tickets.Logging;
using SupportTriage.Domain.TicketAggregate;
using SupportTriage.Domain.TicketAggregate.Interfaces.Repositories;
using SupportTriage.Domain.TicketAggregate.Records;

namespace SupportTriage.Application.Tickets.Commands;

// Imports the seed tickets of every tenant. Safe to run on every start: a ticket that already exists is skipped, so
// triage results saved since the last start are kept. Must run in its own DI scope, because it makes the tenant
// scope unrestricted. Returns the number of tickets added.
public sealed record ImportSeedTicketsCommand : IRequest<int>;

public sealed class ImportSeedTicketsCommandHandler : IRequestHandler<ImportSeedTicketsCommand, int>
{
    private readonly ISeedTicketSource _seedTicketSource;

    private readonly ITicketRepository _ticketRepository;

    private readonly ITenantScope _tenantScope;

    private readonly ILogger<ImportSeedTicketsCommandHandler> _logger;

    public ImportSeedTicketsCommandHandler(ISeedTicketSource seedTicketSource, ITicketRepository ticketRepository, ITenantScope tenantScope, ILogger<ImportSeedTicketsCommandHandler> logger)
    {
        _seedTicketSource = seedTicketSource ?? throw new ArgumentNullException(nameof(seedTicketSource));
        _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
        _tenantScope = tenantScope ?? throw new ArgumentNullException(nameof(tenantScope));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<int> Handle(ImportSeedTicketsCommand request, CancellationToken cancellationToken)
    {
        _tenantScope.AssignUnrestrictedScope();

        var seeds = await _seedTicketSource.ReadSeedTicketsAsync(cancellationToken);
        var imported = 0;
        foreach (var seed in seeds)
        {
            var ticket = Ticket.Import(new TicketImport(seed.Id, seed.TenantId, seed.CustomerEmail, seed.Subject, seed.Body, seed.CreatedAt, seed.Category, seed.Priority, seed.SuggestedReply));
            if (await _ticketRepository.TryAddAsync(ticket, cancellationToken))
                imported++;
        }

        TicketTriageLog.SeedTicketsImported(_logger, imported, seeds.Count);

        return imported;
    }
}
