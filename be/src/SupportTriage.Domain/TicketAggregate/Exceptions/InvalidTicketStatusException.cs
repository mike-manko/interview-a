using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Domain.TicketAggregate.Exceptions;

public sealed class InvalidTicketStatusException : ExceptionBase
{
    public InvalidTicketStatusException(ErrorMessage error) : base(error)
    {
    }

    public InvalidTicketStatusException(IEnumerable<ErrorMessage> errors) : base(errors)
    {
    }

    public InvalidTicketStatusException(ErrorMessage error, Exception innerException) : base(error, innerException)
    {
    }
}
