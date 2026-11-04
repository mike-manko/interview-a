namespace SupportTriage.Domain.TicketAggregate.Enums;

public enum ScreeningVerdict
{
    // Nothing in the ticket tries to instruct the AI; it may be classified.
    Safe = 1,

    // The ticket tries to instruct or manipulate the AI; it is hidden and never sent for classification.
    PromptInjection = 2,
}
