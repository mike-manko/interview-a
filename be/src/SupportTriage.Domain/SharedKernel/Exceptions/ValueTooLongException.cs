namespace SupportTriage.Domain.SharedKernel.Exceptions;

public sealed class ValueTooLongException : ExceptionBase
{
    public ValueTooLongException(ErrorMessage error) : base(error)
    {
    }

    public ValueTooLongException(IEnumerable<ErrorMessage> errors) : base(errors)
    {
    }

    public ValueTooLongException(ErrorMessage error, Exception innerException) : base(error, innerException)
    {
    }
}
