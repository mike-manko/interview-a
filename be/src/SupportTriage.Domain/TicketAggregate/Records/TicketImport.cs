namespace SupportTriage.Domain.TicketAggregate.Records;

// One ticket from an external source (seed data). Category and priority arrive as text and may be outside the
// allowed set; Ticket.Import drops such values so the classification step runs again.
public sealed record TicketImport(
    string Id,
    string TenantId,
    string CustomerEmail,
    string Subject,
    string Body,
    DateTime CreatedAt,
    string? Category,
    string? Priority,
    string? SuggestedReply);
