using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Application.Tickets.Exceptions;

public sealed class TicketAlreadyExistsException : ExceptionBase
{
    public TicketAlreadyExistsException(ErrorMessage error) : base(error)
    {
    }

    public TicketAlreadyExistsException(IEnumerable<ErrorMessage> errors) : base(errors)
    {
    }

    public TicketAlreadyExistsException(ErrorMessage error, Exception innerException) : base(error, innerException)
    {
    }
}
