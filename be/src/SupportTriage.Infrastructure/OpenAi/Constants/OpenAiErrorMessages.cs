using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Infrastructure.OpenAi.Constants;

// Codes 3000-3099 belong to the OpenAI adapter (errors.md).
internal static class OpenAiErrorMessages
{
    public static readonly ErrorMessage ProviderReturnedError = new("OpenAI answered with HTTP {0} after retries.", 3001, ErrorType.Unexpected);

    public static readonly ErrorMessage ProviderUnreachable = new("OpenAI could not be reached after retries.", 3002, ErrorType.Unexpected);

    public static readonly ErrorMessage ProviderTimedOut = new("OpenAI did not answer in time.", 3003, ErrorType.Unexpected);

    public static readonly ErrorMessage ResponseRefused = new("The model refused to classify the ticket: {0}", 3004, ErrorType.Unexpected);

    public static readonly ErrorMessage ResponseIncomplete = new("The model response ended with finish reason {0} instead of Stop.", 3005, ErrorType.Unexpected);

    public static readonly ErrorMessage ResponseIsEmpty = new("The model response is empty.", 3006, ErrorType.Unexpected);

    public static readonly ErrorMessage ResponseIsNotJsonObject = new("The model response is not a JSON object.", 3007, ErrorType.Unexpected);

    public static readonly ErrorMessage ResponseFieldIsMissing = new("The model response has no string field {0}.", 3008, ErrorType.Unexpected);

    public static readonly ErrorMessage ResponseCategoryIsNotAllowed = new("The model returned category '{0}', which is not one of the allowed categories.", 3009, ErrorType.Unexpected);

    public static readonly ErrorMessage ResponsePriorityIsNotAllowed = new("The model returned priority '{0}', which is not one of the allowed priorities.", 3010, ErrorType.Unexpected);

    public static readonly ErrorMessage ScreeningVerdictIsMissing = new("The screening response has no boolean field {0}.", 3012, ErrorType.Unexpected);

    public static readonly ErrorMessage ProviderRejectedConfiguration = new("OpenAI rejected the request with HTTP {0}; check the API key, endpoint and model name.", 3011, ErrorType.Unexpected);
}
