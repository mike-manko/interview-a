using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Domain.TicketAggregate.Exceptions;

// Carries every validation error of a new ticket at once.
public sealed class InvalidTicketException : ExceptionBase
{
    public InvalidTicketException(ErrorMessage error) : base(error)
    {
    }

    public InvalidTicketException(IEnumerable<ErrorMessage> errors) : base(errors)
    {
    }

    public InvalidTicketException(ErrorMessage error, Exception innerException) : base(error, innerException)
    {
    }
}
