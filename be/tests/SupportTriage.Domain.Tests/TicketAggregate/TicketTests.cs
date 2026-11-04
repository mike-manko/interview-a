using SupportTriage.Domain.SharedKernel.Exceptions;
using SupportTriage.Domain.Tests.Fixtures;
using SupportTriage.Domain.Tests.Mocks;
using SupportTriage.Domain.TicketAggregate;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.Exceptions;
using SupportTriage.Domain.TicketAggregate.ValueObjects;

namespace SupportTriage.Domain.Tests.TicketAggregate;

[TestFixture]
public sealed class TicketTests
{
    private const string Source = "openai:test-model:classification-v1";

    private const string FailureNote = "The model returned category 'tech', which is not one of the allowed categories.";

    private const int MaxAttempts = 3;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);

    private static readonly TicketClassification BillingHigh = new(TicketCategory.Billing, TicketPriority.High, Source);

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
    public void Constructor_ValidInput_CreatesPendingUnclassifiedTicket()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_sut.Id, Is.EqualTo(TicketFixture.TicketId));
            Assert.That(_sut.TenantId, Is.EqualTo(TicketFixture.TenantId));
            Assert.That(_sut.Status, Is.EqualTo(TicketStatus.Pending));
            Assert.That(_sut.CreatedAt, Is.EqualTo(FakeDateTime.Start));
            Assert.That(_sut.IsClassified, Is.False);
            Assert.That(_sut.Category, Is.Null);
            Assert.That(_sut.Priority, Is.Null);
            Assert.That(_sut.IsHidden, Is.False);
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void Constructor_BlankTenantId_ThrowsInvalidTicketException(string? tenantId)
    {
        Assert.Throws<InvalidTicketException>(() => _ = new Ticket(new FixedIdGenerator(TicketFixture.TicketId), tenantId!, TicketFixture.CustomerEmail, TicketFixture.Subject, TicketFixture.Body, _dateTime));
    }

    [Test]
    public void Constructor_BlankCustomerEmail_ThrowsInvalidTicketException()
    {
        Assert.Throws<InvalidTicketException>(() => _ = new Ticket(new FixedIdGenerator(TicketFixture.TicketId), TicketFixture.TenantId, " ", TicketFixture.Subject, TicketFixture.Body, _dateTime));
    }

    [Test]
    public void Constructor_SubjectLongerThanMax_ThrowsInvalidTicketException()
    {
        var subject = new string('s', Ticket.MaxSubjectLength + 1);

        Assert.Throws<InvalidTicketException>(() => TicketFixture.Create(_dateTime, subject: subject));
    }

    [Test]
    public void Constructor_BodyLongerThanMax_ThrowsInvalidTicketException()
    {
        var body = new string('b', Ticket.MaxBodyLength + 1);

        Assert.Throws<InvalidTicketException>(() => TicketFixture.Create(_dateTime, body: body));
    }

    [Test]
    public void Constructor_SeveralInvalidFields_ReportsEveryErrorAtOnce()
    {
        var subject = new string('s', Ticket.MaxSubjectLength + 1);

        var exception = Assert.Throws<InvalidTicketException>(() => _ = new Ticket(new FixedIdGenerator(TicketFixture.TicketId), " ", string.Empty, subject, null!, _dateTime));

        Assert.That(Codes(exception!), Is.EqualTo(new[] { 106, 1003, 1006, 1007 }));
    }

    // Seed ticket t-007 has an empty subject and body; the domain accepts it so the seed can be imported.
    [TestCase("", "", false)]
    [TestCase("  ", "\n\t", false)]
    [TestCase("Thanks!", "", true)]
    [TestCase("", "Where is my order?", true)]
    public void HasContent_SubjectAndBody_ReportsWhetherAnyTextExists(string subject, string body, bool expected)
    {
        var ticket = TicketFixture.Create(_dateTime, subject, body);

        Assert.That(ticket.HasContent, Is.EqualTo(expected));
    }

    [Test]
    public void RecordClassification_UnclassifiedTicket_StoresClassificationAndReturnsTrue()
    {
        _dateTime.Advance(TimeSpan.FromMinutes(5));

        var recorded = _sut.RecordClassification(BillingHigh, _dateTime);

        Assert.Multiple(() =>
        {
            Assert.That(recorded, Is.True);
            Assert.That(_sut.IsClassified, Is.True);
            Assert.That(_sut.Category, Is.EqualTo(TicketCategory.Billing));
            Assert.That(_sut.Priority, Is.EqualTo(TicketPriority.High));
            Assert.That(_sut.ClassificationSource, Is.EqualTo(Source));
            Assert.That(_sut.ClassifiedAt, Is.EqualTo(_dateTime.UtcNow));
            Assert.That(_sut.UpdatedAt, Is.EqualTo(_dateTime.UtcNow));
        });
    }

    [Test]
    public void RecordClassification_AlreadyClassified_KeepsFirstClassificationAndReturnsFalse()
    {
        _sut.RecordClassification(BillingHigh, _dateTime);
        var other = new TicketClassification(TicketCategory.Technical, TicketPriority.Critical, "openai:other-model:classification-v2");

        var recorded = _sut.RecordClassification(other, _dateTime);

        Assert.Multiple(() =>
        {
            Assert.That(recorded, Is.False);
            Assert.That(_sut.Category, Is.EqualTo(TicketCategory.Billing));
            Assert.That(_sut.Priority, Is.EqualTo(TicketPriority.High));
            Assert.That(_sut.ClassificationSource, Is.EqualTo(Source));
        });
    }

    [Test]
    public void RecordClassification_AlreadyClassified_DoesNotStampUpdatedAt()
    {
        _sut.RecordClassification(BillingHigh, _dateTime);
        var firstUpdate = _sut.UpdatedAt;
        _dateTime.Advance(TimeSpan.FromMinutes(1));

        _sut.RecordClassification(BillingHigh, _dateTime);

        Assert.That(_sut.UpdatedAt, Is.EqualTo(firstUpdate));
    }

    [Test]
    public void RecordClassification_AfterFailedAttemptBelowMax_StoresClassification()
    {
        _sut.RecordTriageFailure(TriageFailureReason.ProviderUnavailable, FailureNote, MaxAttempts, RetryDelay, _dateTime);

        var recorded = _sut.RecordClassification(BillingHigh, _dateTime);

        Assert.Multiple(() =>
        {
            Assert.That(recorded, Is.True);
            Assert.That(_sut.Category, Is.EqualTo(TicketCategory.Billing));
        });
    }

    [Test]
    public void RecordClassification_HiddenTicket_ThrowsTicketHiddenException()
    {
        _sut.RecordTriageFailure(TriageFailureReason.ProviderUnavailable, FailureNote, 1, RetryDelay, _dateTime);

        Assert.Throws<TicketHiddenException>(() => _sut.RecordClassification(BillingHigh, _dateTime));
    }

    [Test]
    public void RecordClassification_DeletedTicket_ThrowsEntityRemovedException()
    {
        _sut.SoftRemove(_dateTime);

        Assert.Throws<EntityRemovedException>(() => _sut.RecordClassification(BillingHigh, _dateTime));
    }

    [Test]
    public void RecordClassification_NullClassification_ThrowsValueIsRequiredException()
    {
        Assert.Throws<ValueIsRequiredException>(() => _sut.RecordClassification(null!, _dateTime));
    }

    [Test]
    public void RecordTriageFailure_BelowMaxAttempts_CountsAttemptAndKeepsTicketVisible()
    {
        _sut.RecordTriageFailure(TriageFailureReason.ProviderUnavailable, FailureNote, MaxAttempts, RetryDelay, _dateTime);

        Assert.Multiple(() =>
        {
            Assert.That(_sut.FailedTriageAttempts, Is.EqualTo(1));
            Assert.That(_sut.TriageFailureNote, Is.EqualTo(FailureNote));
            Assert.That(_sut.IsHidden, Is.False);
            Assert.That(_sut.Status, Is.EqualTo(TicketStatus.Pending));
            Assert.That(_sut.TriageFailedAt, Is.Null);
        });
    }

    [Test]
    public void RecordTriageFailure_ReachingMaxAttempts_HidesTicketWithLatestNote()
    {
        for (var attempt = 1; attempt < MaxAttempts; attempt++)
            _sut.RecordTriageFailure(TriageFailureReason.ProviderUnavailable, "earlier failure", MaxAttempts, RetryDelay, _dateTime);

        _dateTime.Advance(TimeSpan.FromMinutes(10));
        _sut.RecordTriageFailure(TriageFailureReason.ProviderUnavailable, FailureNote, MaxAttempts, RetryDelay, _dateTime);

        Assert.Multiple(() =>
        {
            Assert.That(_sut.FailedTriageAttempts, Is.EqualTo(MaxAttempts));
            Assert.That(_sut.IsHidden, Is.True);
            Assert.That(_sut.Status, Is.EqualTo(TicketStatus.Failed));
            Assert.That(_sut.TriageFailureNote, Is.EqualTo(FailureNote));
            Assert.That(_sut.TriageFailedAt, Is.EqualTo(_dateTime.UtcNow));
        });
    }

    [Test]
    public void RecordTriageFailure_HiddenTicket_ChangesNothing()
    {
        _sut.RecordTriageFailure(TriageFailureReason.ProviderUnavailable, FailureNote, 1, RetryDelay, _dateTime);
        var hiddenAt = _sut.TriageFailedAt;
        _dateTime.Advance(TimeSpan.FromMinutes(1));

        _sut.RecordTriageFailure(TriageFailureReason.ProviderUnavailable, "a later failure", 1, RetryDelay, _dateTime);

        Assert.Multiple(() =>
        {
            Assert.That(_sut.FailedTriageAttempts, Is.EqualTo(1));
            Assert.That(_sut.TriageFailureNote, Is.EqualTo(FailureNote));
            Assert.That(_sut.TriageFailedAt, Is.EqualTo(hiddenAt));
        });
    }

    [Test]
    public void RecordTriageFailure_NoteLongerThanMax_TruncatesNote()
    {
        var note = new string('n', Ticket.MaxTriageFailureNoteLength + 50);

        _sut.RecordTriageFailure(TriageFailureReason.ProviderUnavailable, note, MaxAttempts, RetryDelay, _dateTime);

        Assert.That(_sut.TriageFailureNote, Has.Length.EqualTo(Ticket.MaxTriageFailureNoteLength));
    }

    [TestCase(Ticket.MinTriageAttempts - 1)]
    [TestCase(Ticket.MaxTriageAttempts + 1)]
    public void RecordTriageFailure_MaxAttemptsOutOfRange_ThrowsValueOutOfRangeException(int maxAttempts)
    {
        Assert.Throws<ValueOutOfRangeException>(() => _sut.RecordTriageFailure(TriageFailureReason.ProviderUnavailable, FailureNote, maxAttempts, RetryDelay, _dateTime));
    }

    [Test]
    public void RecordTriageFailure_BlankNote_ThrowsValueIsRequiredException()
    {
        Assert.Throws<ValueIsRequiredException>(() => _sut.RecordTriageFailure(TriageFailureReason.ProviderUnavailable, " ", MaxAttempts, RetryDelay, _dateTime));
    }

    private static List<int> Codes(InvalidTicketException exception)
    {
        var codes = new List<int>();
        foreach (var error in exception.Errors)
            codes.Add(error.Code);

        return codes;
    }
}
