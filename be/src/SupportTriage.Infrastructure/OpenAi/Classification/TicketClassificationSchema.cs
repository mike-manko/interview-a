using System.Buffers;
using System.Text.Json;
using OpenAI.Chat;
using SupportTriage.Domain.TicketAggregate.Services;

namespace SupportTriage.Infrastructure.OpenAi.Classification;

// Structured Outputs schema for one classification. The enums come from the domain, so the schema cannot drift from
// the allowed set. Strict mode makes the model follow the schema; the response parser still checks every value.
internal static class TicketClassificationSchema
{
    public const string Name = "ticket_classification";

    public const string CategoryProperty = "category";

    public const string PriorityProperty = "priority";

    public static BinaryData Json { get; } = Build();

    public static ChatResponseFormat ResponseFormat { get; } = ChatResponseFormat.CreateJsonSchemaFormat(Name, Json, jsonSchemaFormatDescription: null, jsonSchemaIsStrict: true);

    private static BinaryData Build()
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "object");

            writer.WriteStartObject("properties");
            WriteEnumProperty(writer, CategoryProperty, TriageValueParser.CategoryNames);
            WriteEnumProperty(writer, PriorityProperty, TriageValueParser.PriorityNames);
            writer.WriteEndObject();

            writer.WriteStartArray("required");
            writer.WriteStringValue(CategoryProperty);
            writer.WriteStringValue(PriorityProperty);
            writer.WriteEndArray();

            writer.WriteBoolean("additionalProperties", false);
            writer.WriteEndObject();
        }

        return BinaryData.FromBytes(buffer.WrittenSpan.ToArray());
    }

    private static void WriteEnumProperty(Utf8JsonWriter writer, string name, IReadOnlyList<string> values)
    {
        writer.WriteStartObject(name);
        writer.WriteString("type", "string");
        writer.WriteStartArray("enum");
        foreach (var value in values)
            writer.WriteStringValue(value);

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
