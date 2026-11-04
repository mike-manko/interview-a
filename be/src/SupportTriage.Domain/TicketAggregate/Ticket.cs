using SupportTriage.Domain.SharedKernel;
using SupportTriage.Domain.SharedKernel.Exceptions;
using SupportTriage.Domain.TicketAggregate.Constants;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.Exceptions;
using SupportTriage.Domain.TicketAggregate.Records;
using SupportTriage.Domain.TicketAggregate.Services;
using SupportTriage.Domain.TicketAggregate.ValueObjects;

namespace SupportTriage.Domain.TicketAggregate;

// Triage lifecycle: Pending -> (TryClaim) Processing -> screening -> classification -> (CompleteTriage) Completed.
// Screening runs first: a ticket flagged as prompt injection is hidden at once and never classified.
// A failed attempt returns the ticket to Pending with a later NextAttemptAt; after the last allowed attempt it becomes
// Failed, which hides it. A worker owns a Processing ticket only while its lease has not expired.
public sealed class Ticket : TimestampedTenantEntityBase<string>, IAggregateRoot<string>
{
    public const int MaxTicketIdLength = 64;

    public const int MaxCustomerEmailLength = 254;

    public const int MaxSubjectLength = 200;

    public const int MaxBodyLength = 10_000;

    public const int MaxSuggestedReplyLength = 2_000;

    public const int MaxTriageFailureNoteLength = 1_000;

    public const int MaxWorkerIdLength = 200;

    public const int MinTriageAttempts = 1;

    public const int MaxTriageAttempts = 20;

    public const int MinLeaseSeconds = 1;

    public const int MaxLeaseSeconds = 3_600;

    public const int MaxRetryDelaySeconds = 86_400;

    public const string SeedClassificationSource = "seed";

    public string CustomerEmail { get; private set; } = null!;

    public string Subject { get; private set; } = null!;

    public string Body { get; private set; } = null!;

    public TicketStatus Status { get; private set; }

    public ScreeningVerdict? ScreeningVerdict { get; private set; }

    public string? ScreeningSource { get; private set; }

    public DateTime? ScreenedAt { get; private set; }

    public TicketCategory? Category { get; private set; }

    public TicketPriority? Priority { get; private set; }

    public string? ClassificationSource { get; private set; }

    public DateTime? ClassifiedAt { get; private set; }

    public string? SuggestedReply { get; private set; }

    public int FailedTriageAttempts { get; private set; }

    public string? TriageFailureNote { get; private set; }

    // Why the latest attempt failed; cleared when triage completes.
    public TriageFailureReason? TriageFailureReason { get; private set; }

    public DateTime? TriageFailedAt { get; private set; }

    // The ticket is not claimable before this instant (set on creation and after every failed attempt).
    public DateTime NextAttemptAt { get; private set; }

    public string? LeaseOwner { get; private set; }

    public DateTime? LeaseExpiresAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    // The store's concurrency token; null until the ticket has been saved once.
    public string? Version { get; private set; }

    public bool IsScreened => ScreeningVerdict is not null;

    public bool HasContent => !string.IsNullOrWhiteSpace(Subject) || !string.IsNullOrWhiteSpace(Body);

    // Category and priority are one step: a ticket with only one of them (invalid seed data) counts as unclassified.
    public bool IsClassified => Category is not null && Priority is not null;

    public bool IsHidden => Status == TicketStatus.Failed;

    public bool IsCompleted => Status == TicketStatus.Completed;

    private Ticket()
    {
    }

    // Subject and body may be empty (seed data has such tickets) but not missing; the API rejects a ticket with neither.
    // Card numbers and security codes are masked before anything is stored.
    public Ticket(IIdGenerator<string> idGenerator, string tenantId, string customerEmail, string subject, string body, IDateTime dateTime)
    {
        Guard.EnsureIsNotNull(idGenerator, SharedKernelErrorMessages.IdGeneratorIsRequired);
        Guard.EnsureIsNotNull(dateTime, SharedKernelErrorMessages.DateTimeProviderIsRequired);

        var id = idGenerator.NewId();
        var maskedSubject = subject is null ? null : PaymentCardMasker.Mask(subject);
        var maskedBody = body is null ? null : PaymentCardMasker.Mask(body);
        EnsureValidNewTicket(id, tenantId, customerEmail, maskedSubject, maskedBody);

        // EnsureValidNewTicket has thrown when any of these is null.
        ExecuteCreate(id, tenantId, customerEmail, maskedSubject!, maskedBody!, dateTime.UtcNow);
    }

    // Seed import keeps the source id and creation time. A stored category or priority outside the allowed set is
    // dropped together with its partner, so the classification step runs again; a valid pair completes triage.
    public static Ticket Import(TicketImport import)
    {
        Guard.EnsureIsNotNull(import, TicketErrorMessages.TicketImportIsRequired);

        var createdAt = new FrozenDateTime(import.CreatedAt).UtcNow;
        var maskedSubject = import.Subject is null ? null : PaymentCardMasker.Mask(import.Subject);
        var maskedBody = import.Body is null ? null : PaymentCardMasker.Mask(import.Body);
        EnsureValidNewTicket(import.Id, import.TenantId, import.CustomerEmail, maskedSubject, maskedBody);

        var suggestedReply = string.IsNullOrWhiteSpace(import.SuggestedReply) ? null : import.SuggestedReply;
        if (suggestedReply is not null)
            Guard.EnsureIsNotLongerThan(suggestedReply, MaxSuggestedReplyLength, TicketErrorMessages.SuggestedReplyIsTooLong.WithMessageFormat(MaxSuggestedReplyLength).WithKey(nameof(import.SuggestedReply)));

        var ticket = new Ticket();
        ticket.ExecuteCreate(import.Id, import.TenantId, import.CustomerEmail, maskedSubject!, maskedBody!, createdAt);
        ticket.SuggestedReply = suggestedReply;

        if (TriageValueParser.TryParseCategory(import.Category, out var category) && TriageValueParser.TryParsePriority(import.Priority, out var priority))
        {
            ticket.ExecuteRecordClassification(new TicketClassification(category, priority, SeedClassificationSource), createdAt);
            ticket.ExecuteComplete(createdAt);
        }

        return ticket;
    }

    // Rebuilds a stored ticket. The values were validated when they were written, so only the shape is checked.
    public static Ticket Restore(TicketSnapshot snapshot)
    {
        Guard.EnsureIsNotNull(snapshot, TicketErrorMessages.TicketSnapshotIsRequired);
        Guard.EnsureIsNotNullOrWhiteSpace(snapshot.Version, TicketErrorMessages.VersionIsRequired);

        var ticket = new Ticket
        {
            Id = snapshot.Id,
            TenantId = snapshot.TenantId,
            CustomerEmail = snapshot.CustomerEmail,
            Subject = snapshot.Subject,
            Body = snapshot.Body,
            Status = snapshot.Status,
            ScreeningVerdict = snapshot.ScreeningVerdict,
            ScreeningSource = snapshot.ScreeningSource,
            ScreenedAt = snapshot.ScreenedAt,
            Category = snapshot.Category,
            Priority = snapshot.Priority,
            ClassificationSource = snapshot.ClassificationSource,
            ClassifiedAt = snapshot.ClassifiedAt,
            SuggestedReply = snapshot.SuggestedReply,
            FailedTriageAttempts = snapshot.FailedTriageAttempts,
            TriageFailureNote = snapshot.TriageFailureNote,
            TriageFailureReason = snapshot.TriageFailureReason,
            TriageFailedAt = snapshot.TriageFailedAt,
            NextAttemptAt = snapshot.NextAttemptAt,
            LeaseOwner = snapshot.LeaseOwner,
            LeaseExpiresAt = snapshot.LeaseExpiresAt,
            CompletedAt = snapshot.CompletedAt,
            Version = snapshot.Version,
        };

        ticket.StampCreate(new FrozenDateTime(snapshot.CreatedAt));
        if (snapshot.UpdatedAt is { } updatedAt)
            ticket.StampUpdate(updatedAt);

        if (snapshot.IsDeleted)
            ticket.SoftRemove(new FrozenDateTime(snapshot.DeletedAt ?? snapshot.CreatedAt));

        return ticket;
    }

    // Takes or renews the lease. Returns false when the ticket is not due, is leased by another worker, or has
    // nothing left to do (completed, hidden or removed).
    public bool TryClaim(string workerId, TimeSpan leaseDuration, IDateTime dateTime)
    {
        EnsureValidWorkerId(workerId);
        EnsureDurationInRange(leaseDuration, MinLeaseSeconds, MaxLeaseSeconds, nameof(leaseDuration));
        Guard.EnsureIsNotNull(dateTime, SharedKernelErrorMessages.DateTimeProviderIsRequired);

        if (IsDeleted || IsHidden || IsCompleted)
            return false;

        var now = dateTime.UtcNow;
        if (NextAttemptAt > now)
            return false;

        if (Status == TicketStatus.Processing && LeaseExpiresAt > now && !string.Equals(LeaseOwner, workerId, StringComparison.Ordinal))
            return false;

        ExecuteClaim(workerId, now.Add(leaseDuration));
        StampUpdate(now);

        return true;
    }

    public bool IsLeasedBy(string workerId, IDateTime dateTime)
    {
        Guard.EnsureIsNotNull(dateTime, SharedKernelErrorMessages.DateTimeProviderIsRequired);

        return Status == TicketStatus.Processing
            && string.Equals(LeaseOwner, workerId, StringComparison.Ordinal)
            && LeaseExpiresAt > dateTime.UtcNow;
    }

    // Idempotent: the first recorded screening wins. A ticket flagged as prompt injection is hidden at once, with the
    // reason PromptInjection and a note naming what flagged it. Returns false when nothing changed.
    public bool RecordScreening(TicketScreening screening, IDateTime dateTime)
    {
        Guard.EnsureIsNotNull(screening, TicketErrorMessages.ScreeningIsRequired);
        Guard.EnsureIsNotNull(dateTime, SharedKernelErrorMessages.DateTimeProviderIsRequired);

        EnsureNotDeleted();

        if (IsScreened || IsHidden || IsCompleted)
            return false;

        var screenedAt = dateTime.UtcNow;
        ExecuteRecordScreening(screening, screenedAt);
        StampUpdate(screenedAt);

        return true;
    }

    // Idempotent: the first recorded classification wins, so re-running the step never changes a saved result.
    // Returns false when the ticket was already classified and nothing changed.
    public bool RecordClassification(TicketClassification classification, IDateTime dateTime)
    {
        Guard.EnsureIsNotNull(classification, TicketErrorMessages.ClassificationIsRequired);
        Guard.EnsureIsNotNull(dateTime, SharedKernelErrorMessages.DateTimeProviderIsRequired);

        EnsureNotDeleted();
        EnsureNotHidden();

        if (IsClassified)
            return false;

        EnsureScreened();

        var classifiedAt = dateTime.UtcNow;
        ExecuteRecordClassification(classification, classifiedAt);
        StampUpdate(classifiedAt);

        return true;
    }

    // Ends triage for the worker holding the lease. Repeating it on a completed ticket changes nothing.
    public void CompleteTriage(string workerId, IDateTime dateTime)
    {
        Guard.EnsureIsNotNull(dateTime, SharedKernelErrorMessages.DateTimeProviderIsRequired);

        if (IsCompleted)
            return;

        EnsureNotDeleted();
        EnsureLeasedBy(workerId, dateTime);
        EnsureClassified();

        var completedAt = dateTime.UtcNow;
        ExecuteComplete(completedAt);
        StampUpdate(completedAt);
    }

    // Counts a failed attempt, keeps the latest reason as the note, releases the lease and makes the ticket due again
    // after retryDelay. When the attempts reach maxAttempts the ticket is hidden (Failed) instead. Recording a failure
    // on a hidden or completed ticket changes nothing.
    public void RecordTriageFailure(TriageFailureReason reason, string note, int maxAttempts, TimeSpan retryDelay, IDateTime dateTime)
    {
        EnsureDefinedFailureReason(reason);
        Guard.EnsureIsNotNullOrWhiteSpace(note, TicketErrorMessages.TriageFailureNoteIsRequired);
        Guard.EnsureIsInRange(maxAttempts, MinTriageAttempts, MaxTriageAttempts, SharedKernelErrorMessages.ValueOutOfRange.WithMessageFormat(nameof(maxAttempts), MinTriageAttempts, MaxTriageAttempts, maxAttempts).WithKey(nameof(maxAttempts)));
        EnsureDurationInRange(retryDelay, 0, MaxRetryDelaySeconds, nameof(retryDelay));
        Guard.EnsureIsNotNull(dateTime, SharedKernelErrorMessages.DateTimeProviderIsRequired);

        EnsureNotDeleted();

        if (IsHidden || IsCompleted)
            return;

        var failedAt = dateTime.UtcNow;
        ExecuteRecordTriageFailure(reason, note, maxAttempts, failedAt.Add(retryDelay), failedAt);
        StampUpdate(failedAt);
    }

    // Postpones triage without counting an attempt: the failure was not caused by the ticket (for example a rejected
    // API key), so it must not bring the ticket closer to being hidden.
    public void DeferTriage(TriageFailureReason reason, string note, TimeSpan retryDelay, IDateTime dateTime)
    {
        EnsureDefinedFailureReason(reason);
        Guard.EnsureIsNotNullOrWhiteSpace(note, TicketErrorMessages.TriageFailureNoteIsRequired);
        EnsureDurationInRange(retryDelay, 0, MaxRetryDelaySeconds, nameof(retryDelay));
        Guard.EnsureIsNotNull(dateTime, SharedKernelErrorMessages.DateTimeProviderIsRequired);

        EnsureNotDeleted();

        if (IsHidden || IsCompleted)
            return;

        var deferredAt = dateTime.UtcNow;
        ExecuteDefer(reason, note, deferredAt.Add(retryDelay));
        StampUpdate(deferredAt);
    }

    // Called by persistence after every successful write.
    internal void AcceptVersion(string version)
    {
        Guard.EnsureIsNotNullOrWhiteSpace(version, TicketErrorMessages.VersionIsRequired);

        Version = version;
    }

    private static void EnsureValidNewTicket(string? id, string? tenantId, string? customerEmail, string? subject, string? body)
    {
        List<ErrorMessage>? errors = null;

        if (string.IsNullOrWhiteSpace(id))
            (errors ??= []).Add(TicketErrorMessages.TicketIdIsRequired.WithKey(nameof(id)));
        else if (id.Length > MaxTicketIdLength)
            (errors ??= []).Add(TicketErrorMessages.TicketIdIsTooLong.WithMessageFormat(MaxTicketIdLength).WithKey(nameof(id)));

        if (string.IsNullOrWhiteSpace(tenantId))
            (errors ??= []).Add(SharedKernelErrorMessages.TenantIdIsRequired.WithKey(nameof(tenantId)));
        else if (tenantId.Length > MaxTenantIdLength)
            (errors ??= []).Add(SharedKernelErrorMessages.TenantIdIsTooLong.WithMessageFormat(MaxTenantIdLength).WithKey(nameof(tenantId)));

        if (string.IsNullOrWhiteSpace(customerEmail))
            (errors ??= []).Add(TicketErrorMessages.CustomerEmailIsRequired.WithKey(nameof(customerEmail)));
        else if (customerEmail.Length > MaxCustomerEmailLength)
            (errors ??= []).Add(TicketErrorMessages.CustomerEmailIsTooLong.WithMessageFormat(MaxCustomerEmailLength).WithKey(nameof(customerEmail)));

        if (subject is null)
            (errors ??= []).Add(TicketErrorMessages.SubjectIsRequired.WithKey(nameof(subject)));
        else if (subject.Length > MaxSubjectLength)
            (errors ??= []).Add(TicketErrorMessages.SubjectIsTooLong.WithMessageFormat(MaxSubjectLength).WithKey(nameof(subject)));

        if (body is null)
            (errors ??= []).Add(TicketErrorMessages.BodyIsRequired.WithKey(nameof(body)));
        else if (body.Length > MaxBodyLength)
            (errors ??= []).Add(TicketErrorMessages.BodyIsTooLong.WithMessageFormat(MaxBodyLength).WithKey(nameof(body)));

        if (errors is not null)
            throw new InvalidTicketException(errors);
    }

    private static void EnsureDefinedFailureReason(TriageFailureReason reason)
    {
        if (!Enum.IsDefined(reason))
            throw new ValueOutOfRangeException(TicketErrorMessages.TriageFailureReasonIsNotDefined.WithMessageFormat((int)reason).WithKey(nameof(reason)));
    }

    private static void EnsureValidWorkerId(string workerId)
    {
        Guard.EnsureIsNotNullOrWhiteSpace(workerId, TicketErrorMessages.WorkerIdIsRequired);
        Guard.EnsureIsNotLongerThan(workerId, MaxWorkerIdLength, TicketErrorMessages.WorkerIdIsTooLong.WithMessageFormat(MaxWorkerIdLength).WithKey(nameof(workerId)));
    }

    private static void EnsureDurationInRange(TimeSpan value, int minSeconds, int maxSeconds, string name)
    {
        if (value < TimeSpan.FromSeconds(minSeconds) || value > TimeSpan.FromSeconds(maxSeconds))
            throw new ValueOutOfRangeException(SharedKernelErrorMessages.ValueOutOfRange.WithMessageFormat(name, TimeSpan.FromSeconds(minSeconds), TimeSpan.FromSeconds(maxSeconds), value).WithKey(name));
    }

    private void ExecuteCreate(string id, string tenantId, string customerEmail, string subject, string body, DateTime createdAt)
    {
        Id = id;
        TenantId = tenantId;
        CustomerEmail = customerEmail;
        Subject = subject;
        Body = body;
        Status = TicketStatus.Pending;
        NextAttemptAt = createdAt;
        StampCreate(new FrozenDateTime(createdAt));
    }

    private void ExecuteClaim(string workerId, DateTime leaseExpiresAt)
    {
        Status = TicketStatus.Processing;
        LeaseOwner = workerId;
        LeaseExpiresAt = leaseExpiresAt;
    }

    private void ExecuteRecordScreening(TicketScreening screening, DateTime screenedAt)
    {
        ScreeningVerdict = screening.Verdict;
        ScreeningSource = screening.Source;
        ScreenedAt = screenedAt;

        if (screening.Verdict != Enums.ScreeningVerdict.PromptInjection)
            return;

        Status = TicketStatus.Failed;
        TriageFailureReason = Enums.TriageFailureReason.PromptInjection;
        TriageFailureNote = TruncateNote("Prompt injection detected by " + screening.Source + (screening.Detail is null ? "." : " (" + screening.Detail + ")."));
        TriageFailedAt = screenedAt;
        ReleaseLease();
    }

    private void ExecuteRecordClassification(TicketClassification classification, DateTime classifiedAt)
    {
        Category = classification.Category;
        Priority = classification.Priority;
        ClassificationSource = classification.Source;
        ClassifiedAt = classifiedAt;
    }

    private void ExecuteComplete(DateTime completedAt)
    {
        Status = TicketStatus.Completed;
        CompletedAt = completedAt;
        TriageFailureReason = null;
        ReleaseLease();
    }

    private void ExecuteRecordTriageFailure(TriageFailureReason reason, string note, int maxAttempts, DateTime nextAttemptAt, DateTime failedAt)
    {
        FailedTriageAttempts++;
        TriageFailureReason = reason;
        TriageFailureNote = TruncateNote(note);
        ReleaseLease();

        if (FailedTriageAttempts < maxAttempts)
        {
            Status = TicketStatus.Pending;
            NextAttemptAt = nextAttemptAt;
            return;
        }

        Status = TicketStatus.Failed;
        TriageFailedAt = failedAt;
    }

    private void ExecuteDefer(TriageFailureReason reason, string note, DateTime nextAttemptAt)
    {
        TriageFailureReason = reason;
        TriageFailureNote = TruncateNote(note);
        Status = TicketStatus.Pending;
        NextAttemptAt = nextAttemptAt;
        ReleaseLease();
    }

    private void ReleaseLease()
    {
        LeaseOwner = null;
        LeaseExpiresAt = null;
    }

    private void EnsureNotHidden()
    {
        if (IsHidden)
            throw new TicketHiddenException(TicketErrorMessages.TicketIsHidden.WithMessageFormat(Id).WithKey(Id));
    }

    private void EnsureLeasedBy(string workerId, IDateTime dateTime)
    {
        if (!IsLeasedBy(workerId, dateTime))
            throw new InvalidTicketStatusException(TicketErrorMessages.TicketIsNotLeasedByWorker.WithMessageFormat(Id, workerId).WithKey(Id));
    }

    private void EnsureScreened()
    {
        if (!IsScreened)
            throw new InvalidTicketStatusException(TicketErrorMessages.TicketIsNotScreened.WithMessageFormat(Id).WithKey(Id));
    }

    private void EnsureClassified()
    {
        if (!IsClassified)
            throw new InvalidTicketStatusException(TicketErrorMessages.TicketIsNotClassified.WithMessageFormat(Id).WithKey(Id));
    }

    private static string TruncateNote(string note) => note.Length <= MaxTriageFailureNoteLength ? note : note[..MaxTriageFailureNoteLength];
}
