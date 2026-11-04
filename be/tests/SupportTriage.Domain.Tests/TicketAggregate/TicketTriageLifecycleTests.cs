using SupportTriage.Domain.SharedKernel.Exceptions;
using SupportTriage.Domain.Tests.Fixtures;
using SupportTriage.Domain.Tests.Mocks;
using SupportTriage.Domain.TicketAggregate;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.Exceptions;
using SupportTriage.Domain.TicketAggregate.ValueObjects;

namespace SupportTriage.Domain.Tests.TicketAggregate;

[TestFixture]
public sealed class TicketTriageLifecycleTests
{
    private const string WorkerA = "worker-a";

    private const string WorkerB = "worker-b";

    private const string Note = "OpenAI answered with HTTP 503 after retries.";

    private const int MaxAttempts = 3;

    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(5);

    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(2);

    private static readonly TicketClassification BillingHigh = new(TicketCategory.Billing, TicketPriority.High, "openai:test-model:classification-v1");

    private FakeDateTime _dateTime = null!;

    private Ticket _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dateTime = new FakeDateTime();
        _sut = TicketFixture.Create(_dateTime);
        _sut.RecordScreening(TicketFixture.SafeScreening, _dateTime);
    }

    [Test]
    public void TryClaim_NewTicket_TakesLeaseAndReturnsTrue()
    {
        var claimed = _sut.TryClaim(WorkerA, Lease, _dateTime);

        Assert.Multiple(() =>
        {
            Assert.That(claimed, Is.True);
            Assert.That(_sut.Status, Is.EqualTo(TicketStatus.Processing));
            Assert.That(_sut.LeaseOwner, Is.EqualTo(WorkerA));
            Assert.That(_sut.LeaseExpiresAt, Is.EqualTo(_dateTime.UtcNow + Lease));
            Assert.That(_sut.IsLeasedBy(WorkerA, _dateTime), Is.True);
        });
    }

    [Test]
    public void TryClaim_LeasedByAnotherWorker_ReturnsFalseAndKeepsOwner()
    {
        _sut.TryClaim(WorkerA, Lease, _dateTime);

        var claimed = _sut.TryClaim(WorkerB, Lease, _dateTime);

        Assert.Multiple(() =>
        {
            Assert.That(claimed, Is.False);
            Assert.That(_sut.LeaseOwner, Is.EqualTo(WorkerA));
        });
    }

    // A worker that crashed never releases its lease; once it expires another worker takes over.
    [Test]
    public void TryClaim_ExpiredLeaseOfAnotherWorker_TakesOver()
    {
        _sut.TryClaim(WorkerA, Lease, _dateTime);
        _dateTime.Advance(Lease + TimeSpan.FromSeconds(1));

        var claimed = _sut.TryClaim(WorkerB, Lease, _dateTime);

        Assert.Multiple(() =>
        {
            Assert.That(claimed, Is.True);
            Assert.That(_sut.LeaseOwner, Is.EqualTo(WorkerB));
            Assert.That(_sut.IsLeasedBy(WorkerA, _dateTime), Is.False);
        });
    }

    [Test]
    public void TryClaim_SameWorker_RenewsLease()
    {
        _sut.TryClaim(WorkerA, Lease, _dateTime);
        _dateTime.Advance(TimeSpan.FromMinutes(1));

        var claimed = _sut.TryClaim(WorkerA, Lease, _dateTime);

        Assert.Multiple(() =>
        {
            Assert.That(claimed, Is.True);
            Assert.That(_sut.LeaseExpiresAt, Is.EqualTo(_dateTime.UtcNow + Lease));
        });
    }

    [Test]
    public void TryClaim_BeforeNextAttemptTime_ReturnsFalse()
    {
        _sut.TryClaim(WorkerA, Lease, _dateTime);
        _sut.RecordTriageFailure(TriageFailureReason.ProviderUnavailable, Note, MaxAttempts, RetryDelay, _dateTime);
        _dateTime.Advance(RetryDelay - TimeSpan.FromSeconds(1));

        Assert.That(_sut.TryClaim(WorkerB, Lease, _dateTime), Is.False);
    }

    [Test]
    public void TryClaim_AfterNextAttemptTime_ReturnsTrue()
    {
        _sut.TryClaim(WorkerA, Lease, _dateTime);
        _sut.RecordTriageFailure(TriageFailureReason.ProviderUnavailable, Note, MaxAttempts, RetryDelay, _dateTime);
        _dateTime.Advance(RetryDelay);

        Assert.That(_sut.TryClaim(WorkerB, Lease, _dateTime), Is.True);
    }

    [Test]
    public void TryClaim_CompletedTicket_ReturnsFalse()
    {
        _sut.TryClaim(WorkerA, Lease, _dateTime);
        _sut.RecordClassification(BillingHigh, _dateTime);
        _sut.CompleteTriage(WorkerA, _dateTime);

        Assert.That(_sut.TryClaim(WorkerB, Lease, _dateTime), Is.False);
    }

    [Test]
    public void TryClaim_HiddenTicket_ReturnsFalse()
    {
        _sut.TryClaim(WorkerA, Lease, _dateTime);
        _sut.RecordTriageFailure(TriageFailureReason.ProviderUnavailable, Note, 1, RetryDelay, _dateTime);
        _dateTime.Advance(RetryDelay);

        Assert.That(_sut.TryClaim(WorkerB, Lease, _dateTime), Is.False);
    }

    [Test]
    public void TryClaim_RemovedTicket_ReturnsFalse()
    {
        _sut.SoftRemove(_dateTime);

        Assert.That(_sut.TryClaim(WorkerA, Lease, _dateTime), Is.False);
    }

    [TestCase(0)]
    [TestCase(Ticket.MaxLeaseSeconds + 1)]
    public void TryClaim_LeaseOutOfRange_ThrowsValueOutOfRangeException(int seconds)
    {
        Assert.Throws<ValueOutOfRangeException>(() => _sut.TryClaim(WorkerA, TimeSpan.FromSeconds(seconds), _dateTime));
    }

    [Test]
    public void TryClaim_BlankWorkerId_ThrowsValueIsRequiredException()
    {
        Assert.Throws<ValueIsRequiredException>(() => _sut.TryClaim(" ", Lease, _dateTime));
    }

    [Test]
    public void CompleteTriage_ClassifiedTicketLeasedByWorker_CompletesAndReleasesLease()
    {
        _sut.TryClaim(WorkerA, Lease, _dateTime);
        _sut.RecordClassification(BillingHigh, _dateTime);
        _dateTime.Advance(TimeSpan.FromSeconds(3));

        _sut.CompleteTriage(WorkerA, _dateTime);

        Assert.Multiple(() =>
        {
            Assert.That(_sut.Status, Is.EqualTo(TicketStatus.Completed));
            Assert.That(_sut.CompletedAt, Is.EqualTo(_dateTime.UtcNow));
            Assert.That(_sut.LeaseOwner, Is.Null);
            Assert.That(_sut.LeaseExpiresAt, Is.Null);
        });
    }

    [Test]
    public void CompleteTriage_AlreadyCompleted_ChangesNothing()
    {
        _sut.TryClaim(WorkerA, Lease, _dateTime);
        _sut.RecordClassification(BillingHigh, _dateTime);
        _sut.CompleteTriage(WorkerA, _dateTime);
        var completedAt = _sut.CompletedAt;
        _dateTime.Advance(TimeSpan.FromMinutes(1));

        _sut.CompleteTriage(WorkerB, _dateTime);

        Assert.That(_sut.CompletedAt, Is.EqualTo(completedAt));
    }

    [Test]
    public void CompleteTriage_LeaseExpired_ThrowsInvalidTicketStatusException()
    {
        _sut.TryClaim(WorkerA, Lease, _dateTime);
        _sut.RecordClassification(BillingHigh, _dateTime);
        _dateTime.Advance(Lease);

        Assert.Throws<InvalidTicketStatusException>(() => _sut.CompleteTriage(WorkerA, _dateTime));
    }

    [Test]
    public void CompleteTriage_LeasedByAnotherWorker_ThrowsInvalidTicketStatusException()
    {
        _sut.TryClaim(WorkerA, Lease, _dateTime);
        _sut.RecordClassification(BillingHigh, _dateTime);

        Assert.Throws<InvalidTicketStatusException>(() => _sut.CompleteTriage(WorkerB, _dateTime));
    }

    [Test]
    public void CompleteTriage_Unclassified_ThrowsInvalidTicketStatusException()
    {
        _sut.TryClaim(WorkerA, Lease, _dateTime);

        Assert.Throws<InvalidTicketStatusException>(() => _sut.CompleteTriage(WorkerA, _dateTime));
    }

    [Test]
    public void RecordTriageFailure_BelowMaxAttempts_ReleasesLeaseAndSchedulesNextAttempt()
    {
        _sut.TryClaim(WorkerA, Lease, _dateTime);

        _sut.RecordTriageFailure(TriageFailureReason.ProviderUnavailable, Note, MaxAttempts, RetryDelay, _dateTime);

        Assert.Multiple(() =>
        {
            Assert.That(_sut.Status, Is.EqualTo(TicketStatus.Pending));
            Assert.That(_sut.FailedTriageAttempts, Is.EqualTo(1));
            Assert.That(_sut.NextAttemptAt, Is.EqualTo(_dateTime.UtcNow + RetryDelay));
            Assert.That(_sut.LeaseOwner, Is.Null);
            Assert.That(_sut.TriageFailureNote, Is.EqualTo(Note));
        });
    }

    [Test]
    public void RecordTriageFailure_CompletedTicket_ChangesNothing()
    {
        _sut.TryClaim(WorkerA, Lease, _dateTime);
        _sut.RecordClassification(BillingHigh, _dateTime);
        _sut.CompleteTriage(WorkerA, _dateTime);

        _sut.RecordTriageFailure(TriageFailureReason.ProviderUnavailable, Note, MaxAttempts, RetryDelay, _dateTime);

        Assert.Multiple(() =>
        {
            Assert.That(_sut.Status, Is.EqualTo(TicketStatus.Completed));
            Assert.That(_sut.FailedTriageAttempts, Is.Zero);
        });
    }

    [Test]
    public void DeferTriage_LeasedTicket_PostponesWithoutCountingAnAttempt()
    {
        _sut.TryClaim(WorkerA, Lease, _dateTime);

        _sut.DeferTriage(TriageFailureReason.ProviderConfiguration, Note, RetryDelay, _dateTime);

        Assert.Multiple(() =>
        {
            Assert.That(_sut.Status, Is.EqualTo(TicketStatus.Pending));
            Assert.That(_sut.FailedTriageAttempts, Is.Zero);
            Assert.That(_sut.NextAttemptAt, Is.EqualTo(_dateTime.UtcNow + RetryDelay));
            Assert.That(_sut.LeaseOwner, Is.Null);
            Assert.That(_sut.TriageFailureNote, Is.EqualTo(Note));
        });
    }

    [Test]
    public void DeferTriage_ManyTimes_NeverHidesTheTicket()
    {
        for (var i = 0; i < Ticket.MaxTriageAttempts + 5; i++)
        {
            _sut.TryClaim(WorkerA, Lease, _dateTime);
            _sut.DeferTriage(TriageFailureReason.ProviderConfiguration, Note, TimeSpan.Zero, _dateTime);
        }

        Assert.That(_sut.IsHidden, Is.False);
    }

    [Test]
    public void RecordTriageFailure_Always_StoresTheReasonForOperators()
    {
        _sut.TryClaim(WorkerA, Lease, _dateTime);

        _sut.RecordTriageFailure(TriageFailureReason.InvalidResponse, Note, MaxAttempts, RetryDelay, _dateTime);

        Assert.That(_sut.TriageFailureReason, Is.EqualTo(TriageFailureReason.InvalidResponse));
    }

    [Test]
    public void RecordTriageFailure_LastAttempt_KeepsTheReasonOnTheHiddenTicket()
    {
        _sut.TryClaim(WorkerA, Lease, _dateTime);

        _sut.RecordTriageFailure(TriageFailureReason.InvalidResponse, Note, 1, RetryDelay, _dateTime);

        Assert.Multiple(() =>
        {
            Assert.That(_sut.IsHidden, Is.True);
            Assert.That(_sut.TriageFailureReason, Is.EqualTo(TriageFailureReason.InvalidResponse));
        });
    }

    [Test]
    public void RecordTriageFailure_UndefinedReason_ThrowsValueOutOfRangeException()
    {
        Assert.Throws<ValueOutOfRangeException>(() => _sut.RecordTriageFailure((TriageFailureReason)0, Note, MaxAttempts, RetryDelay, _dateTime));
    }

    [Test]
    public void DeferTriage_Always_StoresTheReason()
    {
        _sut.TryClaim(WorkerA, Lease, _dateTime);

        _sut.DeferTriage(TriageFailureReason.ProviderConfiguration, Note, RetryDelay, _dateTime);

        Assert.That(_sut.TriageFailureReason, Is.EqualTo(TriageFailureReason.ProviderConfiguration));
    }

    [Test]
    public void CompleteTriage_AfterAFailedAttempt_ClearsTheReason()
    {
        _sut.TryClaim(WorkerA, Lease, _dateTime);
        _sut.RecordTriageFailure(TriageFailureReason.ProviderUnavailable, Note, MaxAttempts, TimeSpan.Zero, _dateTime);
        _sut.TryClaim(WorkerA, Lease, _dateTime);
        _sut.RecordClassification(BillingHigh, _dateTime);

        _sut.CompleteTriage(WorkerA, _dateTime);

        Assert.Multiple(() =>
        {
            Assert.That(_sut.TriageFailureReason, Is.Null);
            Assert.That(_sut.FailedTriageAttempts, Is.EqualTo(1));
        });
    }
}
