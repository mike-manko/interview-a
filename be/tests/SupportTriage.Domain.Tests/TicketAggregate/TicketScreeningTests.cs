using SupportTriage.Domain.Tests.Fixtures;
using SupportTriage.Domain.Tests.Mocks;
using SupportTriage.Domain.TicketAggregate;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.Exceptions;
using SupportTriage.Domain.TicketAggregate.Services;
using SupportTriage.Domain.TicketAggregate.ValueObjects;

namespace SupportTriage.Domain.Tests.TicketAggregate;

[TestFixture]
public sealed class TicketScreeningTests
{
    private const string WorkerId = "worker-a";

    private static readonly TicketClassification BillingHigh = new(TicketCategory.Billing, TicketPriority.High, "openai:test-model:classification-v1");

    private FakeDateTime _dateTime = null!;

    private Ticket _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dateTime = new FakeDateTime();
        _sut = TicketFixture.Create(_dateTime);
    }

    [Test]
    public void RecordScreening_Safe_StoresVerdictAndKeepsTicketVisible()
    {
        var recorded = _sut.RecordScreening(TicketFixture.SafeScreening, _dateTime);

        Assert.Multiple(() =>
        {
            Assert.That(recorded, Is.True);
            Assert.That(_sut.IsScreened, Is.True);
            Assert.That(_sut.ScreeningVerdict, Is.EqualTo(ScreeningVerdict.Safe));
            Assert.That(_sut.ScreeningSource, Is.EqualTo(TicketFixture.SafeScreening.Source));
            Assert.That(_sut.ScreenedAt, Is.EqualTo(_dateTime.UtcNow));
            Assert.That(_sut.IsHidden, Is.False);
            Assert.That(_sut.TriageFailureReason, Is.Null);
        });
    }

    // Requirement: a ticket with a prompt injection is hidden at once and marked as prompt injection in its note.
    [Test]
    public void RecordScreening_FlaggedByRule_HidesTicketAndNotesTheRule()
    {
        _sut.TryClaim(WorkerId, TimeSpan.FromMinutes(5), _dateTime);

        _sut.RecordScreening(TicketScreening.FlaggedByRule(PromptInjectionRules.OverrideInstructions), _dateTime);

        Assert.Multiple(() =>
        {
            Assert.That(_sut.IsHidden, Is.True);
            Assert.That(_sut.Status, Is.EqualTo(TicketStatus.Failed));
            Assert.That(_sut.ScreeningVerdict, Is.EqualTo(ScreeningVerdict.PromptInjection));
            Assert.That(_sut.TriageFailureReason, Is.EqualTo(TriageFailureReason.PromptInjection));
            Assert.That(_sut.TriageFailureNote, Is.EqualTo("Prompt injection detected by rule:prompt-injection:override-instructions (override-instructions)."));
            Assert.That(_sut.TriageFailedAt, Is.EqualTo(_dateTime.UtcNow));
            Assert.That(_sut.LeaseOwner, Is.Null);
        });
    }

    [Test]
    public void RecordScreening_FlaggedByModel_NotesTheModel()
    {
        _sut.RecordScreening(new TicketScreening(ScreeningVerdict.PromptInjection, "openai:test-model:screening-v1", null), _dateTime);

        Assert.That(_sut.TriageFailureNote, Is.EqualTo("Prompt injection detected by openai:test-model:screening-v1."));
    }

    [Test]
    public void RecordScreening_AlreadyScreened_KeepsFirstVerdictAndReturnsFalse()
    {
        _sut.RecordScreening(TicketFixture.SafeScreening, _dateTime);

        var recorded = _sut.RecordScreening(TicketScreening.FlaggedByRule(PromptInjectionRules.RoleChange), _dateTime);

        Assert.Multiple(() =>
        {
            Assert.That(recorded, Is.False);
            Assert.That(_sut.ScreeningVerdict, Is.EqualTo(ScreeningVerdict.Safe));
            Assert.That(_sut.IsHidden, Is.False);
        });
    }

    [Test]
    public void RecordClassification_UnscreenedTicket_ThrowsInvalidTicketStatusException()
    {
        Assert.Throws<InvalidTicketStatusException>(() => _sut.RecordClassification(BillingHigh, _dateTime));
    }

    [Test]
    public void RecordClassification_FlaggedTicket_ThrowsTicketHiddenException()
    {
        _sut.RecordScreening(TicketScreening.FlaggedByRule(PromptInjectionRules.PrivilegedMode), _dateTime);

        Assert.Throws<TicketHiddenException>(() => _sut.RecordClassification(BillingHigh, _dateTime));
    }

    [Test]
    public void TryClaim_FlaggedTicket_ReturnsFalse()
    {
        _sut.RecordScreening(TicketScreening.FlaggedByRule(PromptInjectionRules.PrivilegedMode), _dateTime);

        Assert.That(_sut.TryClaim(WorkerId, TimeSpan.FromMinutes(5), _dateTime), Is.False);
    }

    [Test]
    public void Constructor_UndefinedVerdict_ThrowsInvalidTicketScreeningException()
    {
        Assert.Throws<InvalidTicketScreeningException>(() => _ = new TicketScreening((ScreeningVerdict)0, "source", null));
    }

    [Test]
    public void Constructor_BlankSource_ThrowsInvalidTicketScreeningException()
    {
        Assert.Throws<InvalidTicketScreeningException>(() => _ = new TicketScreening(ScreeningVerdict.Safe, " ", null));
    }
}
