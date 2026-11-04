namespace SupportTriage.Application.Tickets.Enums;

public enum ScreenTicketOutcome
{
    // Screened now and found safe; classification may run.
    Safe = 1,

    // Screened now and flagged; the ticket is hidden.
    PromptInjection = 2,

    // A previous run already saved a screening result; nothing was done.
    AlreadyScreened = 3,

    // The ticket is hidden, removed or completed; nothing was done.
    NotApplicable = 4,
}
