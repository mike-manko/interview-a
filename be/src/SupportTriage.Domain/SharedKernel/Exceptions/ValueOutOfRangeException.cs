namespace SupportTriage.Domain.SharedKernel.Exceptions;

public sealed class ValueOutOfRangeException : ExceptionBase
{
    public ValueOutOfRangeException(ErrorMessage error) : base(error)
    {
    }

    public ValueOutOfRangeException(IEnumerable<ErrorMessage> errors) : base(errors)
    {
    }

    public ValueOutOfRangeException(ErrorMessage error, Exception innerException) : base(error, innerException)
    {
    }
}
