using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Application.Tests.Mocks;

internal sealed class FakeDateTime : IDateTime
{
    public DateTime UtcNow { get; private set; } = Start;

    public DateOnly UtcDate => DateOnly.FromDateTime(UtcNow);

    public static readonly DateTime Start = new(2026, 10, 28, 8, 0, 0, DateTimeKind.Utc);

    public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
}
