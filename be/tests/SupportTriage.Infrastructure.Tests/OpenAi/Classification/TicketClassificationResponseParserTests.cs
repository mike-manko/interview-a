using OpenAI.Chat;
using SupportTriage.Application.Tickets.Exceptions;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Infrastructure.OpenAi.Classification;

namespace SupportTriage.Infrastructure.Tests.OpenAi.Classification;

[TestFixture]
public sealed class TicketClassificationResponseParserTests
{
    private const string Source = "openai:test-model:classification-v1";

    [TestCase("""{"category":"Billing","priority":"High"}""", TicketCategory.Billing, TicketPriority.High)]
    [TestCase("""{"category":"Technical","priority":"Critical"}""", TicketCategory.Technical, TicketPriority.Critical)]
    [TestCase("""{"category":"Account","priority":"Medium"}""", TicketCategory.Account, TicketPriority.Medium)]
    [TestCase("""{"category":"Shipping","priority":"Low"}""", TicketCategory.Shipping, TicketPriority.Low)]
    [TestCase("""{ "priority" : "Low", "category" : "Other" }""", TicketCategory.Other, TicketPriority.Low)]
    public void Parse_AllowedValues_ReturnsClassification(string content, TicketCategory category, TicketPriority priority)
    {
        var result = TicketClassificationResponseParser.Parse(content, refusal: null, ChatFinishReason.Stop, Source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Category, Is.EqualTo(category));
            Assert.That(result.Priority, Is.EqualTo(priority));
            Assert.That(result.Source, Is.EqualTo(Source));
        });
    }

    [Test]
    public void Parse_UnknownExtraProperty_IgnoresIt()
    {
        var result = TicketClassificationResponseParser.Parse("""{"category":"Billing","priority":"High","reason":"double charge"}""", refusal: null, ChatFinishReason.Stop, Source);

        Assert.That(result.Category, Is.EqualTo(TicketCategory.Billing));
    }

    // "tech" is seed ticket t-013; the rest are near misses a model can produce.
    [TestCase("tech")]
    [TestCase("billing")]
    [TestCase("Billing ")]
    [TestCase("Refund")]
    [TestCase("")]
    public void Parse_CategoryOutsideAllowedSet_ThrowsInvalidTriageResponseException(string category)
    {
        var content = """{"category":"__C__","priority":"High"}""".Replace("__C__", category, StringComparison.Ordinal);

        var exception = Assert.Throws<InvalidTriageResponseException>(() => TicketClassificationResponseParser.Parse(content, refusal: null, ChatFinishReason.Stop, Source));

        Assert.That(exception!.Errors[0].Code, Is.EqualTo(3009));
    }

    // "Urgent" is seed ticket t-012.
    [TestCase("Urgent")]
    [TestCase("high")]
    [TestCase("P1")]
    public void Parse_PriorityOutsideAllowedSet_ThrowsInvalidTriageResponseException(string priority)
    {
        var content = """{"category":"Shipping","priority":"__P__"}""".Replace("__P__", priority, StringComparison.Ordinal);

        var exception = Assert.Throws<InvalidTriageResponseException>(() => TicketClassificationResponseParser.Parse(content, refusal: null, ChatFinishReason.Stop, Source));

        Assert.That(exception!.Errors[0].Code, Is.EqualTo(3010));
    }

    [Test]
    public void Parse_CategoryAndPriorityOutsideAllowedSet_ReportsBothAtOnce()
    {
        var exception = Assert.Throws<InvalidTriageResponseException>(() => TicketClassificationResponseParser.Parse("""{"category":"tech","priority":"Urgent"}""", refusal: null, ChatFinishReason.Stop, Source));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Errors, Has.Count.EqualTo(2));
            Assert.That(exception.Errors[0].Code, Is.EqualTo(3009));
            Assert.That(exception.Errors[1].Code, Is.EqualTo(3010));
        });
    }

    [Test]
    public void Parse_RejectedValue_ReportsItInTheMessage()
    {
        var exception = Assert.Throws<InvalidTriageResponseException>(() => TicketClassificationResponseParser.Parse("""{"category":"tech","priority":"High"}""", refusal: null, ChatFinishReason.Stop, Source));

        Assert.That(exception!.Message, Does.Contain("'tech'"));
    }

    [Test]
    public void Parse_VeryLongRejectedValue_ShortensItInTheMessage()
    {
        var category = new string('x', 5_000);
        var content = """{"category":"__C__","priority":"High"}""".Replace("__C__", category, StringComparison.Ordinal);

        var exception = Assert.Throws<InvalidTriageResponseException>(() => TicketClassificationResponseParser.Parse(content, refusal: null, ChatFinishReason.Stop, Source));

        Assert.That(exception!.Message, Has.Length.LessThan(300));
    }

    [TestCase("""{"priority":"High"}""")]
    [TestCase("""{"category":"Billing"}""")]
    [TestCase("""{"category":null,"priority":"High"}""")]
    [TestCase("""{"category":1,"priority":"High"}""")]
    [TestCase("""{"category":["Billing"],"priority":"High"}""")]
    [TestCase("""{"Category":"Billing","Priority":"High"}""")]
    public void Parse_MissingOrNonStringField_ThrowsInvalidTriageResponseException(string content)
    {
        var exception = Assert.Throws<InvalidTriageResponseException>(() => TicketClassificationResponseParser.Parse(content, refusal: null, ChatFinishReason.Stop, Source));

        Assert.That(exception!.Errors[0].Code, Is.EqualTo(3008));
    }

    [TestCase("Billing / High")]
    [TestCase("""{"category":"Billing","priority":""")]
    [TestCase("""["Billing","High"]""")]
    [TestCase("\"Billing\"")]
    public void Parse_NotAJsonObject_ThrowsInvalidTriageResponseException(string content)
    {
        var exception = Assert.Throws<InvalidTriageResponseException>(() => TicketClassificationResponseParser.Parse(content, refusal: null, ChatFinishReason.Stop, Source));

        Assert.That(exception!.Errors[0].Code, Is.EqualTo(3007));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void Parse_EmptyContent_ThrowsInvalidTriageResponseException(string? content)
    {
        var exception = Assert.Throws<InvalidTriageResponseException>(() => TicketClassificationResponseParser.Parse(content, refusal: null, ChatFinishReason.Stop, Source));

        Assert.That(exception!.Errors[0].Code, Is.EqualTo(3006));
    }

    [Test]
    public void Parse_Refusal_ThrowsEvenWhenContentLooksValid()
    {
        var exception = Assert.Throws<InvalidTriageResponseException>(() => TicketClassificationResponseParser.Parse("""{"category":"Billing","priority":"High"}""", "I can't help with that.", ChatFinishReason.Stop, Source));

        Assert.That(exception!.Errors[0].Code, Is.EqualTo(3004));
    }

    // A response cut off by the token limit or filtered can still look like valid JSON; it is never trusted.
    [TestCase(ChatFinishReason.Length)]
    [TestCase(ChatFinishReason.ContentFilter)]
    [TestCase(ChatFinishReason.ToolCalls)]
    public void Parse_FinishReasonOtherThanStop_ThrowsInvalidTriageResponseException(ChatFinishReason finishReason)
    {
        var exception = Assert.Throws<InvalidTriageResponseException>(() => TicketClassificationResponseParser.Parse("""{"category":"Billing","priority":"High"}""", refusal: null, finishReason, Source));

        Assert.That(exception!.Errors[0].Code, Is.EqualTo(3005));
    }
}
