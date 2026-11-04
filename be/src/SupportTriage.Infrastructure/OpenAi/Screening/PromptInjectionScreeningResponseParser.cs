using System.Text.Json;
using OpenAI.Chat;
using SupportTriage.Application.Tickets.Dto;
using SupportTriage.Application.Tickets.Exceptions;
using SupportTriage.Infrastructure.OpenAi.Constants;

namespace SupportTriage.Infrastructure.OpenAi.Screening;

// An unusable answer (refusal, cut-off, malformed) is a failed attempt, never "safe": a ticket that could not be
// screened is not classified.
internal static class PromptInjectionScreeningResponseParser
{
    private const int MaxEchoedTextLength = 100;

    public static PromptInjectionScreeningDto Parse(string? content, string? refusal, ChatFinishReason finishReason, string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        if (!string.IsNullOrWhiteSpace(refusal))
            throw new InvalidTriageResponseException(OpenAiErrorMessages.ResponseRefused.WithMessageFormat(refusal.Length <= MaxEchoedTextLength ? refusal : refusal[..MaxEchoedTextLength]));

        if (finishReason != ChatFinishReason.Stop)
            throw new InvalidTriageResponseException(OpenAiErrorMessages.ResponseIncomplete.WithMessageFormat(finishReason));

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidTriageResponseException(OpenAiErrorMessages.ResponseIsEmpty);

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
                throw new InvalidTriageResponseException(OpenAiErrorMessages.ResponseIsNotJsonObject);

            if (!root.TryGetProperty(PromptInjectionScreeningSchema.VerdictProperty, out var verdict) || verdict.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new InvalidTriageResponseException(OpenAiErrorMessages.ScreeningVerdictIsMissing.WithMessageFormat(PromptInjectionScreeningSchema.VerdictProperty));

            return new PromptInjectionScreeningDto(verdict.GetBoolean(), source);
        }
    }
}
