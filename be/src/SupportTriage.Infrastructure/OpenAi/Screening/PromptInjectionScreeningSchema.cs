using System.Buffers;
using System.Text.Json;
using OpenAI.Chat;

namespace SupportTriage.Infrastructure.OpenAi.Screening;

// Structured Outputs schema for one screening verdict: a single required boolean, strict mode.
internal static class PromptInjectionScreeningSchema
{
    public const string Name = "prompt_injection_screening";

    public const string VerdictProperty = "isPromptInjection";

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
            writer.WriteStartObject(VerdictProperty);
            writer.WriteString("type", "boolean");
            writer.WriteEndObject();
            writer.WriteEndObject();

            writer.WriteStartArray("required");
            writer.WriteStringValue(VerdictProperty);
            writer.WriteEndArray();

            writer.WriteBoolean("additionalProperties", false);
            writer.WriteEndObject();
        }

        return BinaryData.FromBytes(buffer.WrittenSpan.ToArray());
    }
}
