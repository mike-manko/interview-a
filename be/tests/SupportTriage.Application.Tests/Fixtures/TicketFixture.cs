using SupportTriage.Application.Tests.Mocks;
using SupportTriage.Domain.TicketAggregate;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.ValueObjects;

namespace SupportTriage.Application.Tests.Fixtures;

internal static class TicketFixture
{
    public const string TicketId = "t-001";

    public const string TenantId = "contoso";

    public const string Subject = "Charged twice for October";

    public const string Body = "Hi, I see two charges of 49 EUR on my card for the October subscription. Please refund one of them.";

    public static Ticket Create(string subject = Subject, string body = Body)
    {
        return new Ticket(new FixedIdGenerator(TicketId), TenantId, "anna.k@example.com", subject, body, new FakeDateTime());
    }

    public static readonly TicketScreening SafeScreening = new(ScreeningVerdict.Safe, "openai:test-model:screening-v1", null);

    public static Ticket CreateScreened(string subject = Subject, string body = Body)
    {
        var ticket = Create(subject, body);
        ticket.RecordScreening(SafeScreening, new FakeDateTime());

        return ticket;
    }

    public static Ticket CreateClassified()
    {
        var ticket = CreateScreened();
        ticket.RecordClassification(new TicketClassification(TicketCategory.Account, TicketPriority.Low, "seed"), new FakeDateTime());

        return ticket;
    }

    public static Ticket CreateHidden()
    {
        var ticket = Create();
        ticket.RecordTriageFailure(TriageFailureReason.ProviderUnavailable, "OpenAI answered with HTTP 500 after retries.", 1, TimeSpan.Zero, new FakeDateTime());

        return ticket;
    }

    public static Ticket CreateRemoved()
    {
        var ticket = Create();
        ticket.SoftRemove(new FakeDateTime());

        return ticket;
    }
}
