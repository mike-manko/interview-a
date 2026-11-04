namespace SupportTriage.Application.Tickets.Policies;

// Exponential backoff between triage attempts: baseDelay * 2^(failedAttempts - 1), capped at maxDelay.
public static class TriageRetryPolicy
{
    private const int MaxExponent = 20;

    public static TimeSpan DelayFor(int failedAttempts, TimeSpan baseDelay, TimeSpan maxDelay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(failedAttempts, 1);

        var exponent = Math.Min(failedAttempts - 1, MaxExponent);
        var delay = baseDelay * (1L << exponent);

        return delay < maxDelay ? delay : maxDelay;
    }
}
