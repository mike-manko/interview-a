using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Domain.TicketAggregate.Exceptions;

// Carries every reason a classification is not allowed (undefined category or priority, missing or too long source).
public sealed class InvalidTicketClassificationException : ExceptionBase
{
    public InvalidTicketClassificationException(ErrorMessage error) : base(error)
    {
    }

    public InvalidTicketClassificationException(IEnumerable<ErrorMessage> errors) : base(errors)
    {
    }

    public InvalidTicketClassificationException(ErrorMessage error, Exception innerException) : base(error, innerException)
    {
    }
}
