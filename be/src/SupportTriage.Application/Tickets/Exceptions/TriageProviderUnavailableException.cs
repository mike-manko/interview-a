using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Application.Tickets.Exceptions;

// The triage provider could not be reached or kept failing after the HTTP retries. Counts as a failed triage attempt.
public sealed class TriageProviderUnavailableException : ExceptionBase
{
    public TriageProviderUnavailableException(ErrorMessage error) : base(error)
    {
    }

    public TriageProviderUnavailableException(IEnumerable<ErrorMessage> errors) : base(errors)
    {
    }

    public TriageProviderUnavailableException(ErrorMessage error, Exception innerException) : base(error, innerException)
    {
    }
}
