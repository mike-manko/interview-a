using SupportTriage.Domain.TicketAggregate.Enums;

namespace SupportTriage.Domain.TicketAggregate.Records;

// Stored state of a ticket, used only by persistence to rebuild the aggregate (Ticket.Restore). Version is the
// store's concurrency token (the Cosmos ETag).
public sealed record TicketSnapshot(
    string Id,
    string TenantId,
    string CustomerEmail,
    string Subject,
    string Body,
    TicketStatus Status,
    ScreeningVerdict? ScreeningVerdict,
    string? ScreeningSource,
    DateTime? ScreenedAt,
    TicketCategory? Category,
    TicketPriority? Priority,
    string? ClassificationSource,
    DateTime? ClassifiedAt,
    string? SuggestedReply,
    int FailedTriageAttempts,
    string? TriageFailureNote,
    TriageFailureReason? TriageFailureReason,
    DateTime? TriageFailedAt,
    DateTime NextAttemptAt,
    string? LeaseOwner,
    DateTime? LeaseExpiresAt,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsDeleted,
    DateTime? DeletedAt,
    string Version);
