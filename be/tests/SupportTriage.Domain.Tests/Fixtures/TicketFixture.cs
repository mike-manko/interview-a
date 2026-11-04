using SupportTriage.Domain.SharedKernel;
using SupportTriage.Domain.Tests.Mocks;
using SupportTriage.Domain.TicketAggregate;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.ValueObjects;

namespace SupportTriage.Domain.Tests.Fixtures;

internal static class TicketFixture
{
    public const string TicketId = "t-001";

    public const string TenantId = "contoso";

    public const string CustomerEmail = "anna.k@example.com";

    public const string Subject = "Charged twice for October";

    public const string Body = "Hi, I see two charges of 49 EUR on my card for the October subscription. Please refund one of them.";

    public static readonly TicketScreening SafeScreening = new(ScreeningVerdict.Safe, "openai:test-model:screening-v1", null);

    public static Ticket Create(IDateTime dateTime, string subject = Subject, string body = Body)
    {
        return new Ticket(new FixedIdGenerator(TicketId), TenantId, CustomerEmail, subject, body, dateTime);
    }
}
