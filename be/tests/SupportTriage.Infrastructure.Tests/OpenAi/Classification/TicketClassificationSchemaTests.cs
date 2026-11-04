using System.Text.Json;
using SupportTriage.Domain.TicketAggregate.Services;
using SupportTriage.Infrastructure.OpenAi.Classification;

namespace SupportTriage.Infrastructure.Tests.OpenAi.Classification;

[TestFixture]
public sealed class TicketClassificationSchemaTests
{
    private JsonDocument _schema = null!;

    [SetUp]
    public void SetUp()
    {
        _schema = JsonDocument.Parse(TicketClassificationSchema.Json.ToString());
    }

    [TearDown]
    public void TearDown()
    {
        _schema.Dispose();
    }

    [Test]
    public void Json_Always_RequiresExactlyCategoryAndPriority()
    {
        var root = _schema.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("type").GetString(), Is.EqualTo("object"));
            Assert.That(Strings(root.GetProperty("required")), Is.EqualTo(new[] { "category", "priority" }));
            Assert.That(root.GetProperty("additionalProperties").GetBoolean(), Is.False);
        });
    }

    [Test]
    public void Json_CategoryProperty_EnumeratesExactlyTheAllowedCategories()
    {
        var category = _schema.RootElement.GetProperty("properties").GetProperty("category");

        Assert.Multiple(() =>
        {
            Assert.That(category.GetProperty("type").GetString(), Is.EqualTo("string"));
            Assert.That(Strings(category.GetProperty("enum")), Is.EqualTo(TriageValueParser.CategoryNames));
        });
    }

    [Test]
    public void Json_PriorityProperty_EnumeratesExactlyTheAllowedPriorities()
    {
        var priority = _schema.RootElement.GetProperty("properties").GetProperty("priority");

        Assert.Multiple(() =>
        {
            Assert.That(priority.GetProperty("type").GetString(), Is.EqualTo("string"));
            Assert.That(Strings(priority.GetProperty("enum")), Is.EqualTo(TriageValueParser.PriorityNames));
        });
    }

    private static List<string?> Strings(JsonElement array)
    {
        var values = new List<string?>();
        foreach (var item in array.EnumerateArray())
            values.Add(item.GetString());

        return values;
    }
}
