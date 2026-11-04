using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Application.Tickets.Exceptions;

// The provider rejected our credentials, permissions or model name. This is a problem with our setup, not with the
// ticket, so it must not count as a failed triage attempt: otherwise a bad API key would hide every ticket.
public sealed class TriageProviderConfigurationException : ExceptionBase
{
    public TriageProviderConfigurationException(ErrorMessage error) : base(error)
    {
    }

    public TriageProviderConfigurationException(IEnumerable<ErrorMessage> errors) : base(errors)
    {
    }

    public TriageProviderConfigurationException(ErrorMessage error, Exception innerException) : base(error, innerException)
    {
    }
}
