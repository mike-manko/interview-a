namespace SupportTriage.Api.Logging;

// Event ids 5000-5099 belong to the API host (cross-cutting.md "Logging").
internal static partial class ApiLog
{
    [LoggerMessage(EventId = 5001, Level = LogLevel.Warning, Message = "Request {Method} {Path} rejected: the X-Tenant-Id header is missing or invalid")]
    public static partial void TenantHeaderRejected(ILogger logger, string method, string path);

    [LoggerMessage(EventId = 5002, Level = LogLevel.Warning, Message = "Request {Path} failed with {Status}, incident {IncidentId}")]
    public static partial void RequestFailed(ILogger logger, Exception exception, string path, int status, string incidentId);

    [LoggerMessage(EventId = 5003, Level = LogLevel.Error, Message = "Request {Path} failed unexpectedly, incident {IncidentId}")]
    public static partial void RequestFailedUnexpectedly(ILogger logger, Exception exception, string path, string incidentId);

    [LoggerMessage(EventId = 5004, Level = LogLevel.Error, Message = "Request {Path} failed after the response started, incident {IncidentId}; no problem details were written")]
    public static partial void ResponseAlreadyStarted(ILogger logger, Exception exception, string path, string incidentId);

    [LoggerMessage(EventId = 5010, Level = LogLevel.Information, Message = "Triage worker {WorkerId} is disabled by settings")]
    public static partial void TriageWorkerDisabled(ILogger logger, string workerId);

    [LoggerMessage(EventId = 5011, Level = LogLevel.Information, Message = "Triage worker {WorkerId} started: every {PollInterval}, up to {BatchSize} tickets, {Parallelism} at a time")]
    public static partial void TriageWorkerStarted(ILogger logger, string workerId, TimeSpan pollInterval, int batchSize, int parallelism);

    [LoggerMessage(EventId = 5012, Level = LogLevel.Error, Message = "Triage worker {WorkerId} poll failed")]
    public static partial void TriagePollFailed(ILogger logger, Exception exception, string workerId);

    [LoggerMessage(EventId = 5013, Level = LogLevel.Information, Message = "Triage of ticket {TicketId} of tenant {TenantId} ended with {Outcome}")]
    public static partial void TicketTriaged(ILogger logger, string ticketId, string tenantId, string outcome);

    [LoggerMessage(EventId = 5014, Level = LogLevel.Warning, Message = "Triage of ticket {TicketId} of tenant {TenantId} failed")]
    public static partial void TicketTriageFailed(ILogger logger, Exception exception, string ticketId, string tenantId);

    [LoggerMessage(EventId = 5015, Level = LogLevel.Error, Message = "The failure of ticket {TicketId} of tenant {TenantId} could not be recorded; the ticket is retried when its lease expires")]
    public static partial void TriageFailureNotRecorded(ILogger logger, Exception exception, string ticketId, string tenantId);

    [LoggerMessage(EventId = 5016, Level = LogLevel.Information, Message = "Seed import is disabled by settings")]
    public static partial void SeedImportDisabled(ILogger logger);
}
