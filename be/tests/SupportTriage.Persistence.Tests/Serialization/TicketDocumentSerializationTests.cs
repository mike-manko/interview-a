using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Persistence.Documents;
using SupportTriage.Persistence.Serialization;

namespace SupportTriage.Persistence.Tests.Serialization;

// Uses the same settings the Cosmos SDK applies (Newtonsoft with camelCase), so the stored shape is checked.
[TestFixture]
public sealed class TicketDocumentSerializationTests
{
    private static readonly JsonSerializerSettings CosmosLikeSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
    };

    [Test]
    public void Serialize_Dates_UseOneFixedWidthUtcFormat()
    {
        var json = Serialize(new DateTime(2026, 10, 28, 8, 12, 0, DateTimeKind.Utc), new DateTime(2026, 10, 28, 8, 12, 0, 500, DateTimeKind.Utc));

        Assert.Multiple(() =>
        {
            Assert.That(json["createdAt"]!.ToString(Formatting.None), Is.EqualTo("\"2026-10-28T08:12:00.0000000Z\""));
            Assert.That(json["nextAttemptAt"]!.ToString(Formatting.None), Is.EqualTo("\"2026-10-28T08:12:00.5000000Z\""));
        });
    }

    // With Newtonsoft's default format "…:00Z" sorts after "…:00.5Z", so nextAttemptAt <= @now would compare wrongly.
    [Test]
    public void ToQueryValue_LaterInstant_SortsAfterEarlierInstantAsText()
    {
        var earlier = UtcDateTimeConverter.ToQueryValue(new DateTime(2026, 10, 28, 8, 12, 0, DateTimeKind.Utc));
        var later = UtcDateTimeConverter.ToQueryValue(new DateTime(2026, 10, 28, 8, 12, 0, 500, DateTimeKind.Utc));

        Assert.That(string.CompareOrdinal(earlier, later), Is.Negative);
    }

    [Test]
    public void Serialize_Enums_AreStoredAsNames()
    {
        var json = Serialize(DateTime.UtcNow, DateTime.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(json["status"]!.Value<string>(), Is.EqualTo("Pending"));
            Assert.That(json["category"]!.Value<string>(), Is.EqualTo("Billing"));
            Assert.That(json["triageFailureReason"]!.Value<string>(), Is.EqualTo("InvalidResponse"));
        });
    }

    [Test]
    public void Serialize_PartitionKeyAndId_UseTheContainerPaths()
    {
        var json = Serialize(DateTime.UtcNow, DateTime.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(json["id"]!.Value<string>(), Is.EqualTo("t-001"));
            Assert.That(json["tenantId"]!.Value<string>(), Is.EqualTo("contoso"));
            Assert.That(json.ContainsKey("_etag"), Is.False);
        });
    }

    [Test]
    public void Deserialize_StoredDocument_ReadsDatesAsUtc()
    {
        const string stored = """{"id":"t-001","tenantId":"contoso","status":"Completed","createdAt":"2026-10-28T08:12:00.0000000Z","nextAttemptAt":"2026-10-28T08:12:00.0000000Z","_etag":"\"00\""}""";

        var document = JsonConvert.DeserializeObject<TicketDocument>(stored, CosmosLikeSettings)!;

        Assert.Multiple(() =>
        {
            Assert.That(document.CreatedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(document.CreatedAt, Is.EqualTo(new DateTime(2026, 10, 28, 8, 12, 0, DateTimeKind.Utc)));
            Assert.That(document.Status, Is.EqualTo(TicketStatus.Completed));
            Assert.That(document.ETag, Is.EqualTo("\"00\""));
        });
    }

    private static JObject Serialize(DateTime createdAt, DateTime nextAttemptAt)
    {
        var document = new TicketDocument
        {
            Id = "t-001",
            TenantId = "contoso",
            CustomerEmail = "anna.k@example.com",
            Subject = "s",
            Body = "b",
            Status = TicketStatus.Pending,
            Category = TicketCategory.Billing,
            TriageFailureReason = TriageFailureReason.InvalidResponse,
            CreatedAt = createdAt,
            NextAttemptAt = nextAttemptAt,
        };

        // Read back without date parsing, so the assertions see the exact stored text.
        using var reader = new JsonTextReader(new StringReader(JsonConvert.SerializeObject(document, CosmosLikeSettings))) { DateParseHandling = DateParseHandling.None };

        return JObject.Load(reader);
    }
}
