namespace SupportTriage.Application.Tickets.Enums;

public enum ClassifyTicketOutcome
{
    // The classifier was asked and its answer was saved.
    Classified = 1,

    // The ticket had no content, so a fixed rule classified it without asking the classifier.
    ClassifiedByRule = 2,

    // A previous run already saved a classification; nothing was done.
    AlreadyClassified = 3,

    // The ticket failed triage earlier and is hidden; nothing was done.
    Hidden = 4,

    // The ticket was removed; nothing was done.
    Removed = 5,
}
