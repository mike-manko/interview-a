namespace SupportTriage.Domain.SharedKernel.Exceptions;

public sealed class EntityRemovedException : ExceptionBase
{
    public EntityRemovedException(ErrorMessage error) : base(error)
    {
    }

    public EntityRemovedException(IEnumerable<ErrorMessage> errors) : base(errors)
    {
    }

    public EntityRemovedException(ErrorMessage error, Exception innerException) : base(error, innerException)
    {
    }
}
