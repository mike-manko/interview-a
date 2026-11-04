using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SupportTriage.Application.Common.Tenancy;
using SupportTriage.Application.Tickets.Commands;
using SupportTriage.Application.Tickets.Dto;
using SupportTriage.Application.Tickets.Interfaces;
using SupportTriage.Domain.TicketAggregate;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.Interfaces.Repositories;

namespace SupportTriage.Application.Tests.Tickets.Commands;

[TestFixture]
public sealed class ImportSeedTicketsCommandHandlerTests
{
    private static readonly DateTime CreatedAt = new(2026, 10, 28, 8, 12, 0, DateTimeKind.Utc);

    private ISeedTicketSource _seedTicketSource = null!;

    private ITicketRepository _ticketRepository = null!;

    private TenantScope _tenantScope = null!;

    private ImportSeedTicketsCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _seedTicketSource = Substitute.For<ISeedTicketSource>();
        _seedTicketSource.ReadSeedTicketsAsync(default).ReturnsForAnyArgs(Task.FromResult<IReadOnlyList<SeedTicketDto>>(
        [
            new SeedTicketDto("t-001", "contoso", "anna.k@example.com", "Charged twice for October", "Two charges.", CreatedAt, null, null, null),
            new SeedTicketDto("t-004", "contoso", "lisa.m@example.com", "Change email address", "How?", CreatedAt, "Account", "Low", "Hi Lisa"),
            new SeedTicketDto("t-101", "fabrikam", "secret.buyer@fabrikam.example.com", "Pricing", "Discount", CreatedAt, null, null, null),
        ]));
        _ticketRepository = Substitute.For<ITicketRepository>();
        _ticketRepository.TryAddAsync(default!, default).ReturnsForAnyArgs(Task.FromResult(true));
        _tenantScope = new TenantScope();

        _sut = new ImportSeedTicketsCommandHandler(_seedTicketSource, _ticketRepository, _tenantScope, NullLogger<ImportSeedTicketsCommandHandler>.Instance);
    }

    [Test]
    public async Task Handle_SeedsOfSeveralTenants_AddsEveryTicketToItsOwnTenant()
    {
        var imported = await _sut.Handle(new ImportSeedTicketsCommand(), CancellationToken.None);

        Assert.That(imported, Is.EqualTo(3));
        await _ticketRepository.Received(1).TryAddAsync(Arg.Is<Ticket>(static ticket => ticket.Id == "t-101" && ticket.TenantId == "fabrikam"), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_Always_UsesAnUnrestrictedScope()
    {
        await _sut.Handle(new ImportSeedTicketsCommand(), CancellationToken.None);

        Assert.That(_tenantScope.HasUnrestrictedAccess, Is.True);
    }

    [Test]
    public async Task Handle_ValidSeedClassification_ImportsCompletedTicket()
    {
        await _sut.Handle(new ImportSeedTicketsCommand(), CancellationToken.None);

        await _ticketRepository.Received(1).TryAddAsync(Arg.Is<Ticket>(static ticket => ticket.Id == "t-004" && ticket.Status == TicketStatus.Completed), Arg.Any<CancellationToken>());
    }

    // Restarting the service must keep triage results saved since the last start.
    [Test]
    public async Task Handle_TicketsAlreadyExist_CountsOnlyNewOnes()
    {
        _ticketRepository.TryAddAsync(default!, default).ReturnsForAnyArgs(Task.FromResult(false), Task.FromResult(true), Task.FromResult(false));

        var imported = await _sut.Handle(new ImportSeedTicketsCommand(), CancellationToken.None);

        Assert.That(imported, Is.EqualTo(1));
    }
}
