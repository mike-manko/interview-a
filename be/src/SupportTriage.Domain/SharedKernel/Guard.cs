using System.Diagnostics.CodeAnalysis;
using SupportTriage.Domain.SharedKernel.Exceptions;

namespace SupportTriage.Domain.SharedKernel;

public static class Guard
{
    public static void EnsureIsNotNull<T>([NotNull] T? value, ErrorMessage error)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(error);

        if (value is null)
            throw new ValueIsRequiredException(error);
    }

    public static void EnsureIsNotNullOrWhiteSpace([NotNull] string? value, ErrorMessage error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (string.IsNullOrWhiteSpace(value))
            throw new ValueIsRequiredException(error);
    }

    public static void EnsureIsNotLongerThan(string value, int maxLength, ErrorMessage error)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(error);

        if (value.Length > maxLength)
            throw new ValueTooLongException(error);
    }

    public static void EnsureHasElements<T>([NotNull] IReadOnlyCollection<T>? value, ErrorMessage error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (value is null || value.Count == 0)
            throw new ValueIsRequiredException(error);
    }

    public static void EnsureIsInRange(int value, int minValue, int maxValue, ErrorMessage error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (value < minValue || value > maxValue)
            throw new ValueOutOfRangeException(error);
    }
}
