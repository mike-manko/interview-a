using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.Services;

namespace SupportTriage.Domain.Tests.TicketAggregate.Services;

[TestFixture]
public sealed class TriageValueParserTests
{
    [Test]
    public void CategoryNames_Always_AreTheFiveAllowedCategories()
    {
        Assert.That(TriageValueParser.CategoryNames, Is.EqualTo(new[] { "Billing", "Technical", "Account", "Shipping", "Other" }));
    }

    [Test]
    public void PriorityNames_Always_AreTheFourAllowedPrioritiesFromLowest()
    {
        Assert.That(TriageValueParser.PriorityNames, Is.EqualTo(new[] { "Low", "Medium", "High", "Critical" }));
    }

    [Test]
    public void CategoryNames_CastToArray_CannotBeChanged()
    {
        Assert.That(TriageValueParser.CategoryNames as string[], Is.Null);
    }

    [TestCase("Billing", TicketCategory.Billing)]
    [TestCase("Technical", TicketCategory.Technical)]
    [TestCase("Account", TicketCategory.Account)]
    [TestCase("Shipping", TicketCategory.Shipping)]
    [TestCase("Other", TicketCategory.Other)]
    public void TryParseCategory_AllowedName_ReturnsCategory(string value, TicketCategory expected)
    {
        var parsed = TriageValueParser.TryParseCategory(value, out var category);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.True);
            Assert.That(category, Is.EqualTo(expected));
        });
    }

    // "tech" is seed ticket t-013. The others are what Enum.TryParse would wrongly accept.
    [TestCase("tech")]
    [TestCase("billing")]
    [TestCase("BILLING")]
    [TestCase(" Billing")]
    [TestCase("Billing ")]
    [TestCase("1")]
    [TestCase("0")]
    [TestCase("Billing,Technical")]
    [TestCase("Billing, Technical")]
    [TestCase("Refund")]
    [TestCase("")]
    public void TryParseCategory_ValueOutsideAllowedSet_ReturnsFalse(string value)
    {
        var parsed = TriageValueParser.TryParseCategory(value, out var category);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.False);
            Assert.That(category, Is.EqualTo(default(TicketCategory)));
        });
    }

    [Test]
    public void TryParseCategory_Null_ReturnsFalse()
    {
        Assert.That(TriageValueParser.TryParseCategory(null, out _), Is.False);
    }

    [TestCase("Low", TicketPriority.Low)]
    [TestCase("Medium", TicketPriority.Medium)]
    [TestCase("High", TicketPriority.High)]
    [TestCase("Critical", TicketPriority.Critical)]
    public void TryParsePriority_AllowedName_ReturnsPriority(string value, TicketPriority expected)
    {
        var parsed = TriageValueParser.TryParsePriority(value, out var priority);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.True);
            Assert.That(priority, Is.EqualTo(expected));
        });
    }

    // "Urgent" is seed ticket t-012.
    [TestCase("Urgent")]
    [TestCase("high")]
    [TestCase("CRITICAL")]
    [TestCase("P1")]
    [TestCase("4")]
    [TestCase("High,Critical")]
    [TestCase(" Low")]
    [TestCase("")]
    public void TryParsePriority_ValueOutsideAllowedSet_ReturnsFalse(string value)
    {
        var parsed = TriageValueParser.TryParsePriority(value, out var priority);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.False);
            Assert.That(priority, Is.EqualTo(default(TicketPriority)));
        });
    }

    [Test]
    public void TryParsePriority_Null_ReturnsFalse()
    {
        Assert.That(TriageValueParser.TryParsePriority(null, out _), Is.False);
    }
}
