using SupportTriage.Domain.TicketAggregate.Enums;

namespace SupportTriage.Application.Tickets.Dto;

// IsHidden is true when triage gave up (status Failed); such tickets are listed only on request. FailureReason and
// FailureMessage explain the latest failed attempt, both while triage is being retried and after it gave up; they are
// null when nothing failed or triage completed.
public sealed record TicketDto(
    string Id,
    string CustomerEmail,
    string Subject,
    string Body,
    TicketStatus Status,
    TicketCategory? Category,
    TicketPriority? Priority,
    string? SuggestedReply,
    DateTime CreatedAt,
    DateTime? ClassifiedAt,
    bool IsHidden,
    int FailedTriageAttempts,
    TriageFailureReason? FailureReason,
    string? FailureMessage);
