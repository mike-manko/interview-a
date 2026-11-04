using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.Exceptions;
using SupportTriage.Domain.TicketAggregate.ValueObjects;

namespace SupportTriage.Domain.Tests.TicketAggregate.ValueObjects;

[TestFixture]
public sealed class TicketClassificationTests
{
    private const string Source = "openai:test-model:classification-v1";

    [Test]
    public void Constructor_AllowedValues_SetsProperties()
    {
        var classification = new TicketClassification(TicketCategory.Billing, TicketPriority.High, Source);

        Assert.Multiple(() =>
        {
            Assert.That(classification.Category, Is.EqualTo(TicketCategory.Billing));
            Assert.That(classification.Priority, Is.EqualTo(TicketPriority.High));
            Assert.That(classification.Source, Is.EqualTo(Source));
        });
    }

    // 0 is default(TicketCategory): an unset value must never pass as a category.
    [TestCase(0)]
    [TestCase(6)]
    [TestCase(-1)]
    public void Constructor_UndefinedCategory_ThrowsInvalidTicketClassificationException(int category)
    {
        Assert.Throws<InvalidTicketClassificationException>(() => _ = new TicketClassification((TicketCategory)category, TicketPriority.Low, Source));
    }

    [TestCase(0)]
    [TestCase(5)]
    public void Constructor_UndefinedPriority_ThrowsInvalidTicketClassificationException(int priority)
    {
        Assert.Throws<InvalidTicketClassificationException>(() => _ = new TicketClassification(TicketCategory.Other, (TicketPriority)priority, Source));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_BlankSource_ThrowsInvalidTicketClassificationException(string? source)
    {
        Assert.Throws<InvalidTicketClassificationException>(() => _ = new TicketClassification(TicketCategory.Other, TicketPriority.Low, source!));
    }

    [Test]
    public void Constructor_SourceLongerThanMax_ThrowsInvalidTicketClassificationException()
    {
        var source = new string('s', TicketClassification.MaxSourceLength + 1);

        var exception = Assert.Throws<InvalidTicketClassificationException>(() => _ = new TicketClassification(TicketCategory.Other, TicketPriority.Low, source));

        Assert.That(exception!.Errors[0].Code, Is.EqualTo(1017));
    }

    [Test]
    public void Constructor_SourceAtMaxLength_KeepsSourceUnchanged()
    {
        var source = new string('s', TicketClassification.MaxSourceLength);

        var classification = new TicketClassification(TicketCategory.Other, TicketPriority.Low, source);

        Assert.That(classification.Source, Is.EqualTo(source));
    }

    [Test]
    public void Constructor_UndefinedCategoryAndPriority_ReportsBothErrorsAtOnce()
    {
        var exception = Assert.Throws<InvalidTicketClassificationException>(() => _ = new TicketClassification((TicketCategory)0, (TicketPriority)0, Source));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Errors, Has.Count.EqualTo(2));
            Assert.That(exception.Errors[0].Code, Is.EqualTo(1010));
            Assert.That(exception.Errors[1].Code, Is.EqualTo(1011));
        });
    }

    [Test]
    public void ForEmptyTicket_Always_IsOtherWithLowPriorityFromTheRule()
    {
        var classification = TicketClassification.ForEmptyTicket;

        Assert.Multiple(() =>
        {
            Assert.That(classification.Category, Is.EqualTo(TicketCategory.Other));
            Assert.That(classification.Priority, Is.EqualTo(TicketPriority.Low));
            Assert.That(classification.Source, Is.EqualTo(TicketClassification.EmptyTicketRuleSource));
        });
    }
}
