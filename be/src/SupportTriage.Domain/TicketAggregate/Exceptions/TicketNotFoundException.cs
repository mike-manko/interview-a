using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Domain.TicketAggregate.Exceptions;

public sealed class TicketNotFoundException : ExceptionBase
{
    public TicketNotFoundException(ErrorMessage error) : base(error)
    {
    }

    public TicketNotFoundException(IEnumerable<ErrorMessage> errors) : base(errors)
    {
    }

    public TicketNotFoundException(ErrorMessage error, Exception innerException) : base(error, innerException)
    {
    }
}
