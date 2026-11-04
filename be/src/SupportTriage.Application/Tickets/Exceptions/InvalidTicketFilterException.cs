using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Application.Tickets.Exceptions;

public sealed class InvalidTicketFilterException : ExceptionBase
{
    public InvalidTicketFilterException(ErrorMessage error) : base(error)
    {
    }

    public InvalidTicketFilterException(IEnumerable<ErrorMessage> errors) : base(errors)
    {
    }

    public InvalidTicketFilterException(ErrorMessage error, Exception innerException) : base(error, innerException)
    {
    }
}
