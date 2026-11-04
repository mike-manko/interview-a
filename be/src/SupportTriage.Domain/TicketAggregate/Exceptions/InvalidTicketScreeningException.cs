using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Domain.TicketAggregate.Exceptions;

// Carries every reason a screening result is not valid.
public sealed class InvalidTicketScreeningException : ExceptionBase
{
    public InvalidTicketScreeningException(ErrorMessage error) : base(error)
    {
    }

    public InvalidTicketScreeningException(IEnumerable<ErrorMessage> errors) : base(errors)
    {
    }

    public InvalidTicketScreeningException(ErrorMessage error, Exception innerException) : base(error, innerException)
    {
    }
}
