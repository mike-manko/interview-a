using System.Text.Json;
using OpenAI.Chat;
using SupportTriage.Application.Tickets.Dto;
using SupportTriage.Application.Tickets.Exceptions;
using SupportTriage.Domain.SharedKernel;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.Services;
using SupportTriage.Infrastructure.OpenAi.Constants;

namespace SupportTriage.Infrastructure.OpenAi.Classification;

// Turns the model answer into allowed values or throws. Strict mode makes a schema violation unlikely, not impossible:
// refusals, truncated output (finish reason Length) and content filtering all bypass the schema.
internal static class TicketClassificationResponseParser
{
    private const int MaxEchoedTextLength = 100;

    public static TicketClassificationDto Parse(string? content, string? refusal, ChatFinishReason finishReason, string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        if (!string.IsNullOrWhiteSpace(refusal))
            throw Invalid(OpenAiErrorMessages.ResponseRefused.WithMessageFormat(Shorten(refusal)));

        if (finishReason != ChatFinishReason.Stop)
            throw Invalid(OpenAiErrorMessages.ResponseIncomplete.WithMessageFormat(finishReason));

        if (string.IsNullOrWhiteSpace(content))
            throw Invalid(OpenAiErrorMessages.ResponseIsEmpty);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(content);
        }
        catch (JsonException ex)
        {
            throw new InvalidTriageResponseException(OpenAiErrorMessages.ResponseIsNotJsonObject, ex);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw Invalid(OpenAiErrorMessages.ResponseIsNotJsonObject);

            // Both fields are checked before throwing, so the failure note shows every bad value.
            List<ErrorMessage>? errors = null;
            var categoryText = ReadString(root, TicketClassificationSchema.CategoryProperty);
            var priorityText = ReadString(root, TicketClassificationSchema.PriorityProperty);

            TicketCategory category = default;
            if (categoryText is null)
                (errors ??= []).Add(OpenAiErrorMessages.ResponseFieldIsMissing.WithMessageFormat(TicketClassificationSchema.CategoryProperty));
            else if (!TriageValueParser.TryParseCategory(categoryText, out category))
                (errors ??= []).Add(OpenAiErrorMessages.ResponseCategoryIsNotAllowed.WithMessageFormat(Shorten(categoryText)));

            TicketPriority priority = default;
            if (priorityText is null)
                (errors ??= []).Add(OpenAiErrorMessages.ResponseFieldIsMissing.WithMessageFormat(TicketClassificationSchema.PriorityProperty));
            else if (!TriageValueParser.TryParsePriority(priorityText, out priority))
                (errors ??= []).Add(OpenAiErrorMessages.ResponsePriorityIsNotAllowed.WithMessageFormat(Shorten(priorityText)));

            if (errors is not null)
                throw new InvalidTriageResponseException(errors);

            return new TicketClassificationDto(category, priority, source);
        }
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
            return null;

        return element.GetString();
    }

    private static string Shorten(string value) => value.Length <= MaxEchoedTextLength ? value : value[..MaxEchoedTextLength];

    private static InvalidTriageResponseException Invalid(ErrorMessage error) => new(error);
}
