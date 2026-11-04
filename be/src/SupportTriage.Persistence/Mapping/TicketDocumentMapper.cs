using SupportTriage.Application.Tickets.Constants;
using SupportTriage.Application.Tickets.Dto;
using SupportTriage.Domain.TicketAggregate;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.Records;
using SupportTriage.Persistence.Documents;

namespace SupportTriage.Persistence.Mapping;

internal static class TicketDocumentMapper
{
    public static TicketDocument ToDocument(Ticket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        return new TicketDocument
        {
            Id = ticket.Id,
            TenantId = ticket.TenantId,
            CustomerEmail = ticket.CustomerEmail,
            Subject = ticket.Subject,
            Body = ticket.Body,
            Status = ticket.Status,
            ScreeningVerdict = ticket.ScreeningVerdict,
            ScreeningSource = ticket.ScreeningSource,
            ScreenedAt = ticket.ScreenedAt,
            Category = ticket.Category,
            Priority = ticket.Priority,
            ClassificationSource = ticket.ClassificationSource,
            ClassifiedAt = ticket.ClassifiedAt,
            SuggestedReply = ticket.SuggestedReply,
            FailedTriageAttempts = ticket.FailedTriageAttempts,
            TriageFailureNote = ticket.TriageFailureNote,
            TriageFailureReason = ticket.TriageFailureReason,
            TriageFailedAt = ticket.TriageFailedAt,
            NextAttemptAt = ticket.NextAttemptAt,
            LeaseOwner = ticket.LeaseOwner,
            LeaseExpiresAt = ticket.LeaseExpiresAt,
            CompletedAt = ticket.CompletedAt,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            IsDeleted = ticket.IsDeleted,
            DeletedAt = ticket.DeletedAt,
        };
    }

    public static Ticket ToTicket(TicketDocument document, string etag)
    {
        ArgumentNullException.ThrowIfNull(document);

        return Ticket.Restore(new TicketSnapshot(
            document.Id,
            document.TenantId,
            document.CustomerEmail,
            document.Subject,
            document.Body,
            document.Status,
            document.ScreeningVerdict,
            document.ScreeningSource,
            document.ScreenedAt,
            document.Category,
            document.Priority,
            document.ClassificationSource,
            document.ClassifiedAt,
            document.SuggestedReply,
            document.FailedTriageAttempts,
            document.TriageFailureNote,
            document.TriageFailureReason,
            document.TriageFailedAt,
            document.NextAttemptAt,
            document.LeaseOwner,
            document.LeaseExpiresAt,
            document.CompletedAt,
            document.CreatedAt,
            document.UpdatedAt,
            document.IsDeleted,
            document.DeletedAt,
            etag));
    }

    public static TicketDto ToDto(TicketDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var isHidden = document.Status == TicketStatus.Failed;
        var failureReason = document.Status == TicketStatus.Completed ? null : document.TriageFailureReason;

        return new TicketDto(
            document.Id,
            document.CustomerEmail,
            document.Subject,
            document.Body,
            document.Status,
            document.Category,
            document.Priority,
            document.SuggestedReply,
            document.CreatedAt,
            document.ClassifiedAt,
            isHidden,
            document.FailedTriageAttempts,
            failureReason,
            failureReason is { } reason ? TriageFailureMessages.For(reason, isHidden) : null);
    }
}
