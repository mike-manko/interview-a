namespace SupportTriage.Domain.TicketAggregate.Enums;

// Why the latest triage attempt failed, in terms an operator can act on. The technical detail stays in the internal
// failure note; the API shows a fixed message per reason.
public enum TriageFailureReason
{
    // The AI provider could not be reached or kept failing after retries.
    ProviderUnavailable = 1,

    // The AI answered, but not with an allowed category and priority (including refusals and cut-off answers).
    InvalidResponse = 2,

    // The AI provider rejected our API key, permissions or model name; not the ticket's fault.
    ProviderConfiguration = 3,

    // Anything else, for example a storage error.
    Unexpected = 4,

    // Screening found text that tries to instruct the AI, so the ticket was hidden before classification.
    PromptInjection = 5,
}
