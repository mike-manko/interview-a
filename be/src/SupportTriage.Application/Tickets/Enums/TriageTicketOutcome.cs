namespace SupportTriage.Application.Tickets.Enums;

public enum TriageTicketOutcome
{
    // Every step is done and the ticket is Completed.
    Completed = 1,

    // The ticket is not due, is leased by another worker, or has nothing left to do.
    NotClaimable = 2,

    // Another worker saved the ticket between our read and our claim; it owns the ticket now.
    ClaimLost = 3,

    // The ticket changed after we claimed it (our lease expired and another worker took over); our work was not saved.
    LeaseLost = 4,

    // The ticket no longer exists in the tenant.
    NotFound = 5,

    // Screening flagged the ticket as prompt injection; it is hidden and was not classified.
    FlaggedAsPromptInjection = 6,
}
