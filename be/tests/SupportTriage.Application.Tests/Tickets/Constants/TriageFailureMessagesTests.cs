using SupportTriage.Application.Tickets.Constants;
using SupportTriage.Domain.TicketAggregate.Enums;

namespace SupportTriage.Application.Tests.Tickets.Constants;

[TestFixture]
public sealed class TriageFailureMessagesTests
{
    [TestCase(TriageFailureReason.ProviderUnavailable, TriageFailureMessages.ProviderUnavailable)]
    [TestCase(TriageFailureReason.InvalidResponse, TriageFailureMessages.InvalidResponse)]
    [TestCase(TriageFailureReason.ProviderConfiguration, TriageFailureMessages.ProviderConfiguration)]
    [TestCase(TriageFailureReason.Unexpected, TriageFailureMessages.Unexpected)]
    public void For_EveryReason_StartsWithItsOwnMessage(TriageFailureReason reason, string expected)
    {
        Assert.That(TriageFailureMessages.For(reason, isHidden: false), Does.StartWith(expected));
    }

    [Test]
    public void For_HiddenTicket_AsksForManualClassification()
    {
        Assert.That(TriageFailureMessages.For(TriageFailureReason.InvalidResponse, isHidden: true), Does.EndWith(TriageFailureMessages.HiddenSuffix));
    }

    [Test]
    public void For_TicketStillRetrying_SaysItWillBeRetried()
    {
        Assert.That(TriageFailureMessages.For(TriageFailureReason.ProviderUnavailable, isHidden: false), Does.EndWith(TriageFailureMessages.RetryingSuffix));
    }

    [Test]
    public void For_EveryReason_HasADistinctMessage()
    {
        var messages = new HashSet<string>(StringComparer.Ordinal);
        foreach (var reason in Enum.GetValues<TriageFailureReason>())
            Assert.That(messages.Add(TriageFailureMessages.For(reason, isHidden: true)), Is.True, reason.ToString());
    }
}
