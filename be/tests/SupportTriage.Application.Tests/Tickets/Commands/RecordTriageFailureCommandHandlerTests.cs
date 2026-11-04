using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SupportTriage.Application.Tests.Fixtures;
using SupportTriage.Application.Tests.Mocks;
using SupportTriage.Application.Tickets.Commands;
using SupportTriage.Application.Tickets.Settings;
using SupportTriage.Domain.TicketAggregate;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.Interfaces.Repositories;

namespace SupportTriage.Application.Tests.Tickets.Commands;

[TestFixture]
public sealed class RecordTriageFailureCommandHandlerTests
{
    private const string WorkerId = "worker-a";

    private const string Note = "OpenAI answered with HTTP 503 after retries.";

    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(5);

    private static readonly TicketTriageSettings Settings = new()
    {
        MaxAttempts = 2,
        RetryBaseDelay = TimeSpan.FromSeconds(30),
        MaxRetryDelay = TimeSpan.FromMinutes(30),
        ConfigurationErrorRetryDelay = TimeSpan.FromMinutes(5),
    };

    private FakeDateTime _dateTime = null!;

    private ITicketRepository _ticketRepository = null!;

    private RecordTriageFailureCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dateTime = new FakeDateTime();
        _ticketRepository = Substitute.For<ITicketRepository>();
        _ticketRepository.TryReplaceAsync(default!, default).ReturnsForAnyArgs(Task.FromResult(true));

        _sut = new RecordTriageFailureCommandHandler(_ticketRepository, _dateTime, Options.Create(Settings), NullLogger<RecordTriageFailureCommandHandler>.Instance);
    }

    [Test]
    public async Task Handle_FirstFailure_SchedulesRetryAfterBaseDelay()
    {
        var ticket = ArrangeClaimed();

        var saved = await _sut.Handle(Command(TriageFailureReason.ProviderUnavailable, countsAsAttempt: true), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(saved, Is.True);
            Assert.That(ticket.Status, Is.EqualTo(TicketStatus.Pending));
            Assert.That(ticket.FailedTriageAttempts, Is.EqualTo(1));
            Assert.That(ticket.NextAttemptAt, Is.EqualTo(_dateTime.UtcNow + Settings.RetryBaseDelay));
            Assert.That(ticket.TriageFailureReason, Is.EqualTo(TriageFailureReason.ProviderUnavailable));
            Assert.That(ticket.TriageFailureNote, Is.EqualTo(Note));
        });
    }

    // Decision: after the last allowed attempt the ticket is hidden and keeps a note.
    [Test]
    public async Task Handle_LastAllowedAttempt_HidesTicketWithReason()
    {
        var ticket = ArrangeClaimed();
        await _sut.Handle(Command(TriageFailureReason.InvalidResponse, countsAsAttempt: true), CancellationToken.None);
        _dateTime.Advance(Settings.RetryBaseDelay);
        ticket.TryClaim(WorkerId, Lease, _dateTime);

        await _sut.Handle(Command(TriageFailureReason.InvalidResponse, countsAsAttempt: true), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(ticket.IsHidden, Is.True);
            Assert.That(ticket.TriageFailureReason, Is.EqualTo(TriageFailureReason.InvalidResponse));
            Assert.That(ticket.FailedTriageAttempts, Is.EqualTo(Settings.MaxAttempts));
        });
    }

    [Test]
    public async Task Handle_ConfigurationFailure_DefersWithoutCountingAnAttempt()
    {
        var ticket = ArrangeClaimed();

        await _sut.Handle(Command(TriageFailureReason.ProviderConfiguration, countsAsAttempt: false), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(ticket.FailedTriageAttempts, Is.Zero);
            Assert.That(ticket.NextAttemptAt, Is.EqualTo(_dateTime.UtcNow + Settings.ConfigurationErrorRetryDelay));
            Assert.That(ticket.TriageFailureReason, Is.EqualTo(TriageFailureReason.ProviderConfiguration));
        });
    }

    // A failure reported by a worker that no longer holds the lease (or reported twice) changes nothing.
    [Test]
    public async Task Handle_WorkerDoesNotHoldLease_ChangesNothing()
    {
        var ticket = TicketFixture.Create();
        ticket.TryClaim("worker-b", Lease, _dateTime);
        Arrange(ticket);

        var saved = await _sut.Handle(Command(TriageFailureReason.ProviderUnavailable, countsAsAttempt: true), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(saved, Is.False);
            Assert.That(ticket.FailedTriageAttempts, Is.Zero);
        });
        await _ticketRepository.DidNotReceiveWithAnyArgs().TryReplaceAsync(default!, default);
    }

    [Test]
    public async Task Handle_SameFailureReportedTwice_CountsItOnce()
    {
        var ticket = ArrangeClaimed();

        await _sut.Handle(Command(TriageFailureReason.ProviderUnavailable, countsAsAttempt: true), CancellationToken.None);
        await _sut.Handle(Command(TriageFailureReason.ProviderUnavailable, countsAsAttempt: true), CancellationToken.None);

        Assert.That(ticket.FailedTriageAttempts, Is.EqualTo(1));
    }

    [Test]
    public async Task Handle_MissingTicket_ReturnsFalse()
    {
        _ticketRepository.FindAsync(TicketFixture.TicketId, Arg.Any<CancellationToken>()).Returns(new ValueTask<Ticket?>((Ticket?)null));

        var saved = await _sut.Handle(Command(TriageFailureReason.Unexpected, countsAsAttempt: true), CancellationToken.None);

        Assert.That(saved, Is.False);
    }

    private static RecordTriageFailureCommand Command(TriageFailureReason reason, bool countsAsAttempt)
    {
        return new RecordTriageFailureCommand(TicketFixture.TicketId, WorkerId, reason, Note, countsAsAttempt);
    }

    private Ticket ArrangeClaimed()
    {
        var ticket = TicketFixture.Create();
        ticket.TryClaim(WorkerId, Lease, _dateTime);

        return Arrange(ticket);
    }

    private Ticket Arrange(Ticket ticket)
    {
        _ticketRepository.FindAsync(TicketFixture.TicketId, Arg.Any<CancellationToken>()).Returns(new ValueTask<Ticket?>(ticket));

        return ticket;
    }
}
