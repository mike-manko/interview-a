using SupportTriage.Domain.SharedKernel.Exceptions;

namespace SupportTriage.Domain.SharedKernel;

// A stamp fixed at one instant, so every timestamp of one operation is identical.
public sealed class FrozenDateTime : IDateTime
{
    public DateTime UtcNow { get; }

    public DateOnly UtcDate => DateOnly.FromDateTime(UtcNow);

    public FrozenDateTime(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
            throw new ValueOutOfRangeException(SharedKernelErrorMessages.DateTimeMustBeUtc.WithMessageFormat(utcNow.Kind).WithKey(nameof(utcNow)));

        UtcNow = utcNow;
    }

    public static FrozenDateTime From(IDateTime dateTime)
    {
        Guard.EnsureIsNotNull(dateTime, SharedKernelErrorMessages.DateTimeProviderIsRequired);

        return new FrozenDateTime(dateTime.UtcNow);
    }
}
