using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Application.Common.Exceptions;

public sealed class TenantScopeException : ExceptionBase
{
    public TenantScopeException(ErrorMessage error) : base(error)
    {
    }

    public TenantScopeException(IEnumerable<ErrorMessage> errors) : base(errors)
    {
    }

    public TenantScopeException(ErrorMessage error, Exception innerException) : base(error, innerException)
    {
    }
}
