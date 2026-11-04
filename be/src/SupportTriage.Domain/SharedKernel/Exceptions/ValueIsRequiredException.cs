namespace SupportTriage.Domain.SharedKernel.Exceptions;

public sealed class ValueIsRequiredException : ExceptionBase
{
    public ValueIsRequiredException(ErrorMessage error) : base(error)
    {
    }

    public ValueIsRequiredException(IEnumerable<ErrorMessage> errors) : base(errors)
    {
    }

    public ValueIsRequiredException(ErrorMessage error, Exception innerException) : base(error, innerException)
    {
    }
}
