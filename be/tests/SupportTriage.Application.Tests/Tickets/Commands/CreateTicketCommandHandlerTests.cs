using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SupportTriage.Application.Common.Exceptions;
using SupportTriage.Application.Common.Interfaces;
using SupportTriage.Application.Common.Tenancy;
using SupportTriage.Application.Tests.Mocks;
using SupportTriage.Application.Tickets.Commands;
using SupportTriage.Application.Tickets.Exceptions;
using SupportTriage.Domain.TicketAggregate;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.Exceptions;
using SupportTriage.Domain.TicketAggregate.Interfaces.Repositories;

namespace SupportTriage.Application.Tests.Tickets.Commands;

[TestFixture]
public sealed class CreateTicketCommandHandlerTests
{
    private const string TicketId = "01a1a70a-9d69-77f2-8439-34d54964929b";

    private ITicketRepository _ticketRepository = null!;

    private ITenantScope _tenantScope = null!;

    private CreateTicketCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _ticketRepository = Substitute.For<ITicketRepository>();
        _ticketRepository.TryAddAsync(default!, default).ReturnsForAnyArgs(Task.FromResult(true));
        _tenantScope = new TenantScope();
        _tenantScope.AssignScope("contoso");

        _sut = new CreateTicketCommandHandler(_ticketRepository, _tenantScope, new FixedIdGenerator(TicketId), new FakeDateTime(), NullLogger<CreateTicketCommandHandler>.Instance);
    }

    [Test]
    public async Task Handle_ValidTicket_StoresPendingTicketInScopeTenant()
    {
        var created = await _sut.Handle(new CreateTicketCommand(" anna.k@example.com ", "Charged twice", "Two charges of 49 EUR."), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(created.Id, Is.EqualTo(TicketId));
            Assert.That(created.Status, Is.EqualTo(TicketStatus.Pending));
        });
        await _ticketRepository.Received(1).TryAddAsync(
            Arg.Is<Ticket>(static ticket => ticket.TenantId == "contoso" && ticket.CustomerEmail == "anna.k@example.com" && ticket.Subject == "Charged twice"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_OnlyBody_StoresEmptySubject()
    {
        await _sut.Handle(new CreateTicketCommand("anna.k@example.com", null, "Where is my order?"), CancellationToken.None);

        await _ticketRepository.Received(1).TryAddAsync(Arg.Is<Ticket>(static ticket => ticket.Subject.Length == 0), Arg.Any<CancellationToken>());
    }

    [TestCase(null, null)]
    [TestCase(" ", "")]
    public void Handle_NoSubjectAndNoBody_ThrowsInvalidTicketException(string? subject, string? body)
    {
        Assert.ThrowsAsync<InvalidTicketException>(async () => await _sut.Handle(new CreateTicketCommand("anna.k@example.com", subject, body), CancellationToken.None));
    }

    [Test]
    public void Handle_NoTenantScope_ThrowsTenantScopeException()
    {
        var sut = new CreateTicketCommandHandler(_ticketRepository, new TenantScope(), new FixedIdGenerator(TicketId), new FakeDateTime(), NullLogger<CreateTicketCommandHandler>.Instance);

        Assert.ThrowsAsync<TenantScopeException>(async () => await sut.Handle(new CreateTicketCommand("anna.k@example.com", "s", "b"), CancellationToken.None));
    }

    [Test]
    public void Handle_IdAlreadyExists_ThrowsTicketAlreadyExistsException()
    {
        _ticketRepository.TryAddAsync(default!, default).ReturnsForAnyArgs(Task.FromResult(false));

        Assert.ThrowsAsync<TicketAlreadyExistsException>(async () => await _sut.Handle(new CreateTicketCommand("anna.k@example.com", "s", "b"), CancellationToken.None));
    }
}
