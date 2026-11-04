using SupportTriage.Application.Tickets.Policies;

namespace SupportTriage.Application.Tests.Tickets.Policies;

[TestFixture]
public sealed class TriageRetryPolicyTests
{
    private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan MaxDelay = TimeSpan.FromMinutes(30);

    [TestCase(1, 30)]
    [TestCase(2, 60)]
    [TestCase(3, 120)]
    [TestCase(4, 240)]
    public void DelayFor_Attempt_DoublesTheDelay(int failedAttempts, int expectedSeconds)
    {
        Assert.That(TriageRetryPolicy.DelayFor(failedAttempts, BaseDelay, MaxDelay), Is.EqualTo(TimeSpan.FromSeconds(expectedSeconds)));
    }

    [TestCase(7)]
    [TestCase(1_000)]
    public void DelayFor_ManyAttempts_IsCappedAtMaxDelay(int failedAttempts)
    {
        Assert.That(TriageRetryPolicy.DelayFor(failedAttempts, BaseDelay, MaxDelay), Is.EqualTo(MaxDelay));
    }

    [Test]
    public void DelayFor_ZeroAttempts_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TriageRetryPolicy.DelayFor(0, BaseDelay, MaxDelay));
    }
}
