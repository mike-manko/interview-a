using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Persistence.Serialization;

namespace SupportTriage.Persistence.Documents;

// One ticket in the tickets container, partitioned by /tenantId. Enums are stored as names so documents stay
// readable and queries compare names, not numbers.
internal sealed class TicketDocument
{
    public string Id { get; set; } = null!;

    public string TenantId { get; set; } = null!;

    public string CustomerEmail { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string Body { get; set; } = null!;

    [JsonConverter(typeof(StringEnumConverter))]
    public TicketStatus Status { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public ScreeningVerdict? ScreeningVerdict { get; set; }

    public string? ScreeningSource { get; set; }

    [JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? ScreenedAt { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public TicketCategory? Category { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public TicketPriority? Priority { get; set; }

    public string? ClassificationSource { get; set; }

    [JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? ClassifiedAt { get; set; }

    public string? SuggestedReply { get; set; }

    public int FailedTriageAttempts { get; set; }

    public string? TriageFailureNote { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public TriageFailureReason? TriageFailureReason { get; set; }

    [JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? TriageFailedAt { get; set; }

    [JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime NextAttemptAt { get; set; }

    public string? LeaseOwner { get; set; }

    [JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? LeaseExpiresAt { get; set; }

    [JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? CompletedAt { get; set; }

    [JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime CreatedAt { get; set; }

    [JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    [JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? DeletedAt { get; set; }

    [JsonProperty("_etag", NullValueHandling = NullValueHandling.Ignore)]
    public string? ETag { get; set; }
}
