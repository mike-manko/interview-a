using SupportTriage.Domain.TicketAggregate.Enums;

namespace SupportTriage.Application.Tickets.Constants;

// Fixed, operator-facing explanations of a failed triage, shown by the front end next to the ticket. They never
// include provider error text, which can contain internal details.
public static class TriageFailureMessages
{
    public const string ProviderUnavailable = "Automatic classification failed: the AI service could not be reached after several attempts.";

    public const string InvalidResponse = "Automatic classification failed: the AI did not return an allowed category and priority.";

    public const string ProviderConfiguration = "Automatic classification is paused: the AI service rejected our configuration (API key or model). An administrator needs to fix the settings.";

    public const string Unexpected = "Automatic classification failed because of an unexpected error.";

    public const string PromptInjection = "Flagged as a possible prompt injection: the ticket contains text that tries to instruct the AI, so it was not sent for automatic classification.";

    public const string HiddenSuffix = " Triage has stopped; please classify this ticket manually.";

    public const string RetryingSuffix = " Triage will be retried automatically.";

    public static string For(TriageFailureReason reason, bool isHidden)
    {
        var message = reason switch
        {
            TriageFailureReason.ProviderUnavailable => ProviderUnavailable,
            TriageFailureReason.InvalidResponse => InvalidResponse,
            TriageFailureReason.ProviderConfiguration => ProviderConfiguration,
            TriageFailureReason.PromptInjection => PromptInjection,
            _ => Unexpected,
        };

        return message + (isHidden ? HiddenSuffix : RetryingSuffix);
    }
}
