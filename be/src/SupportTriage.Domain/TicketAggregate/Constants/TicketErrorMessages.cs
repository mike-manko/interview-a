using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Domain.TicketAggregate.Constants;

// Codes 1000-1999 belong to the Ticket aggregate (errors.md).
internal static class TicketErrorMessages
{
    public static readonly ErrorMessage CustomerEmailIsRequired = new("A customer email is required.", 1003);

    public static readonly ErrorMessage CustomerEmailIsTooLong = new("The customer email must not be longer than {0} characters.", 1004);

    public static readonly ErrorMessage SubjectIsRequired = new("A subject is required; it may be empty but not missing.", 1005);

    public static readonly ErrorMessage SubjectIsTooLong = new("The subject must not be longer than {0} characters.", 1006);

    public static readonly ErrorMessage BodyIsRequired = new("A body is required; it may be empty but not missing.", 1007);

    public static readonly ErrorMessage BodyIsTooLong = new("The body must not be longer than {0} characters.", 1008);

    public static readonly ErrorMessage ClassificationIsRequired = new("A classification is required.", 1009);

    public static readonly ErrorMessage CategoryIsNotAllowed = new("Category {0} is not one of the allowed categories.", 1010);

    public static readonly ErrorMessage PriorityIsNotAllowed = new("Priority {0} is not one of the allowed priorities.", 1011);

    public static readonly ErrorMessage ClassificationSourceIsRequired = new("A classification source is required.", 1012);

    public static readonly ErrorMessage TicketIsHidden = new("Ticket {0} failed triage and is hidden.", 1013, ErrorType.Conflict);

    public static readonly ErrorMessage TriageFailureNoteIsRequired = new("A triage failure note is required.", 1014);

    public static readonly ErrorMessage TicketWithIdNotFound = new("Ticket {0} has not been found.", 1015, ErrorType.NotFound);

    public static readonly ErrorMessage TicketIdIsRequired = new("A ticket id is required.", 1016);

    public static readonly ErrorMessage ClassificationSourceIsTooLong = new("The classification source must not be longer than {0} characters.", 1017);

    public static readonly ErrorMessage WorkerIdIsRequired = new("A worker id is required.", 1018);

    public static readonly ErrorMessage WorkerIdIsTooLong = new("The worker id must not be longer than {0} characters.", 1019);

    public static readonly ErrorMessage TicketIsNotLeasedByWorker = new("Ticket {0} is not leased by worker {1}.", 1020, ErrorType.Conflict);

    public static readonly ErrorMessage TicketIsNotClassified = new("Ticket {0} cannot complete triage before it is classified.", 1021, ErrorType.Conflict);

    public static readonly ErrorMessage TicketIdIsTooLong = new("The ticket id must not be longer than {0} characters.", 1022);

    public static readonly ErrorMessage SuggestedReplyIsTooLong = new("The suggested reply must not be longer than {0} characters.", 1023);

    public static readonly ErrorMessage TicketImportIsRequired = new("A ticket import is required.", 1024);

    public static readonly ErrorMessage TicketSnapshotIsRequired = new("A ticket snapshot is required.", 1025);

    public static readonly ErrorMessage VersionIsRequired = new("A version is required.", 1026);

    public static readonly ErrorMessage TriageFailureReasonIsNotDefined = new("Triage failure reason {0} is not defined.", 1027);

    public static readonly ErrorMessage ScreeningVerdictIsNotDefined = new("Screening verdict {0} is not defined.", 1028);

    public static readonly ErrorMessage ScreeningSourceIsRequired = new("A screening source is required.", 1029);

    public static readonly ErrorMessage ScreeningSourceIsTooLong = new("The screening source must not be longer than {0} characters.", 1030);

    public static readonly ErrorMessage ScreeningIsRequired = new("A screening result is required.", 1031);

    public static readonly ErrorMessage TicketIsNotScreened = new("Ticket {0} cannot be classified before it is screened for prompt injection.", 1032, ErrorType.Conflict);
}
