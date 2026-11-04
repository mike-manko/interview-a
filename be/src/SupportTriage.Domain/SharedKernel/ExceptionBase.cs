namespace SupportTriage.Domain.SharedKernel;

public abstract class ExceptionBase : Exception
{
    public IReadOnlyList<ErrorMessage> Errors { get; }

    public ErrorType ErrorType { get; }

    public string IncidentId { get; }

    protected ExceptionBase(ErrorMessage error) : this(Single(error), null)
    {
    }

    protected ExceptionBase(IEnumerable<ErrorMessage> errors) : this(Materialize(errors), null)
    {
    }

    protected ExceptionBase(ErrorMessage error, Exception innerException) : this(Single(error), innerException)
    {
    }

    private ExceptionBase(IReadOnlyList<ErrorMessage> errors, Exception? innerException) : base(errors[0].Message, innerException)
    {
        Errors = errors;
        ErrorType = errors[0].Type;
        IncidentId = IncidentIdGenerator.Next();
    }

    private static IReadOnlyList<ErrorMessage> Single(ErrorMessage error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return [error];
    }

    private static IReadOnlyList<ErrorMessage> Materialize(IEnumerable<ErrorMessage> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var list = new List<ErrorMessage>();
        foreach (var error in errors)
        {
            ArgumentNullException.ThrowIfNull(error, nameof(errors));
            list.Add(error);
        }

        if (list.Count == 0)
            throw new ArgumentException("At least one error message is required.", nameof(errors));

        return list;
    }
}
