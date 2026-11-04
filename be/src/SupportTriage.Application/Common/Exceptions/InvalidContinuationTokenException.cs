using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Application.Common.Exceptions;

public sealed class InvalidContinuationTokenException : ExceptionBase
{
    public InvalidContinuationTokenException(ErrorMessage error) : base(error)
    {
    }

    public InvalidContinuationTokenException(IEnumerable<ErrorMessage> errors) : base(errors)
    {
    }

    public InvalidContinuationTokenException(ErrorMessage error, Exception innerException) : base(error, innerException)
    {
    }
}
