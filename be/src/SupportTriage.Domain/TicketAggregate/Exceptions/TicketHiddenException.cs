using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Domain.TicketAggregate.Exceptions;

public sealed class TicketHiddenException : ExceptionBase
{
    public TicketHiddenException(ErrorMessage error) : base(error)
    {
    }

    public TicketHiddenException(IEnumerable<ErrorMessage> errors) : base(errors)
    {
    }

    public TicketHiddenException(ErrorMessage error, Exception innerException) : base(error, innerException)
    {
    }
}
