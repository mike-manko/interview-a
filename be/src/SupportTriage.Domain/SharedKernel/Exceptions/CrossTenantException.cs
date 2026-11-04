namespace SupportTriage.Domain.SharedKernel.Exceptions;

public sealed class CrossTenantException : ExceptionBase
{
    public CrossTenantException(ErrorMessage error) : base(error)
    {
    }

    public CrossTenantException(IEnumerable<ErrorMessage> errors) : base(errors)
    {
    }

    public CrossTenantException(ErrorMessage error, Exception innerException) : base(error, innerException)
    {
    }
}
