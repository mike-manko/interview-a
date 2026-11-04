using Microsoft.Extensions.Logging;
using SupportTriage.Domain.TicketAggregate.Enums;

namespace SupportTriage.Application.Tickets.Logging;

// Event ids 2000-2099 belong to tickets in the Application layer (cross-cutting.md "Logging").
internal static partial class TicketTriageLog
{
    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Ticket {TicketId} of tenant {TenantId} classified as {Category}/{Priority} by {Source}")]
    public static partial void TicketClassified(ILogger logger, string ticketId, string tenantId, TicketCategory category, TicketPriority priority, string source);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Ticket {TicketId} of tenant {TenantId} is already classified; classification skipped")]
    public static partial void TicketAlreadyClassified(ILogger logger, string ticketId, string tenantId);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Warning, Message = "Ticket {TicketId} of tenant {TenantId} is hidden after failed triage; classification skipped")]
    public static partial void HiddenTicketSkipped(ILogger logger, string ticketId, string tenantId);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Ticket {TicketId} of tenant {TenantId} is removed; classification skipped")]
    public static partial void RemovedTicketSkipped(ILogger logger, string ticketId, string tenantId);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = "Ticket {TicketId} of tenant {TenantId} created")]
    public static partial void TicketCreated(ILogger logger, string ticketId, string tenantId);

    [LoggerMessage(EventId = 2006, Level = LogLevel.Information, Message = "Ticket {TicketId} of tenant {TenantId} completed triage")]
    public static partial void TriageCompleted(ILogger logger, string ticketId, string tenantId);

    [LoggerMessage(EventId = 2007, Level = LogLevel.Debug, Message = "Ticket {TicketId} of tenant {TenantId} is not claimable by worker {WorkerId}")]
    public static partial void TicketNotClaimable(ILogger logger, string ticketId, string tenantId, string workerId);

    [LoggerMessage(EventId = 2008, Level = LogLevel.Information, Message = "Worker {WorkerId} lost ticket {TicketId} of tenant {TenantId} to another writer during {Stage}")]
    public static partial void TicketLostToAnotherWriter(ILogger logger, string workerId, string ticketId, string tenantId, string stage);

    [LoggerMessage(EventId = 2009, Level = LogLevel.Warning, Message = "Triage attempt {Attempt} of {MaxAttempts} failed for ticket {TicketId} of tenant {TenantId}; next attempt at {NextAttemptAt}: {Note}")]
    public static partial void TriageAttemptFailed(ILogger logger, int attempt, int maxAttempts, string ticketId, string tenantId, DateTime nextAttemptAt, string note);

    [LoggerMessage(EventId = 2010, Level = LogLevel.Error, Message = "Ticket {TicketId} of tenant {TenantId} is hidden after {Attempts} failed triage attempts: {Note}")]
    public static partial void TicketHiddenAfterFailedTriage(ILogger logger, string ticketId, string tenantId, int attempts, string note);

    [LoggerMessage(EventId = 2011, Level = LogLevel.Error, Message = "Triage of ticket {TicketId} of tenant {TenantId} deferred until {NextAttemptAt} without counting an attempt: {Note}")]
    public static partial void TriageDeferred(ILogger logger, string ticketId, string tenantId, DateTime nextAttemptAt, string note);

    [LoggerMessage(EventId = 2012, Level = LogLevel.Information, Message = "Seed import added {Imported} of {Total} tickets; the rest already existed")]
    public static partial void SeedTicketsImported(ILogger logger, int imported, int total);

    [LoggerMessage(EventId = 2013, Level = LogLevel.Warning, Message = "Ticket {TicketId} of tenant {TenantId} flagged as prompt injection by {Source}; hidden before classification")]
    public static partial void PromptInjectionDetected(ILogger logger, string ticketId, string tenantId, string source);

    [LoggerMessage(EventId = 2014, Level = LogLevel.Information, Message = "Ticket {TicketId} of tenant {TenantId} screened as safe by {Source}")]
    public static partial void TicketScreenedSafe(ILogger logger, string ticketId, string tenantId, string source);
}
