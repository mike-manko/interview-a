namespace SupportTriage.Domain.SharedKernel;

// The clock. Aggregates receive it as a method parameter and never read DateTime.UtcNow themselves (domain.md "Time and identity").
public interface IDateTime
{
    DateTime UtcNow { get; }

    DateOnly UtcDate { get; }
}
