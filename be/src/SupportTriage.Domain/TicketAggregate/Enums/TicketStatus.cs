namespace SupportTriage.Domain.TicketAggregate.Enums;

public enum TicketStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,

    // Triage gave up after the allowed attempts: the ticket is hidden from the ticket list and carries a failure note.
    Failed = 4,
}
