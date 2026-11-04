using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Application.Tickets.Exceptions;

// The triage provider answered, but the answer is not usable: a refusal, a cut-off or malformed response, or a value
// outside the allowed set. Counts as a failed triage attempt.
public sealed class InvalidTriageResponseException : ExceptionBase
{
    public InvalidTriageResponseException(ErrorMessage error) : base(error)
    {
    }

    public InvalidTriageResponseException(IEnumerable<ErrorMessage> errors) : base(errors)
    {
    }

    public InvalidTriageResponseException(ErrorMessage error, Exception innerException) : base(error, innerException)
    {
    }
}
