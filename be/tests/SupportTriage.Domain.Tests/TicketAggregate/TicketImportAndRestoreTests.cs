using SupportTriage.Domain.SharedKernel.Exceptions;
using SupportTriage.Domain.Tests.Fixtures;
using SupportTriage.Domain.Tests.Mocks;
using SupportTriage.Domain.TicketAggregate;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.Exceptions;
using SupportTriage.Domain.TicketAggregate.Records;

namespace SupportTriage.Domain.Tests.TicketAggregate;

[TestFixture]
public sealed class TicketImportAndRestoreTests
{
    private static readonly DateTime CreatedAt = new(2026, 10, 28, 9, 30, 0, DateTimeKind.Utc);

    // Seed ticket t-004: a valid category and priority complete triage at import.
    [Test]
    public void Import_ValidCategoryAndPriority_ImportsCompletedTicket()
    {
        var ticket = Ticket.Import(Seed("Account", "Low", "Hi Lisa, you can change your email under Settings > Profile."));

        Assert.Multiple(() =>
        {
            Assert.That(ticket.Id, Is.EqualTo("t-004"));
            Assert.That(ticket.CreatedAt, Is.EqualTo(CreatedAt));
            Assert.That(ticket.Status, Is.EqualTo(TicketStatus.Completed));
            Assert.That(ticket.Category, Is.EqualTo(TicketCategory.Account));
            Assert.That(ticket.Priority, Is.EqualTo(TicketPriority.Low));
            Assert.That(ticket.ClassificationSource, Is.EqualTo(Ticket.SeedClassificationSource));
            Assert.That(ticket.SuggestedReply, Does.StartWith("Hi Lisa"));
        });
    }

    // Seed tickets t-012 ("Urgent") and t-013 ("tech"): one bad value drops the pair, so classification runs again.
    [TestCase("Shipping", "Urgent")]
    [TestCase("tech", "Medium")]
    [TestCase(null, null)]
    [TestCase("Billing", null)]
    public void Import_CategoryOrPriorityOutsideAllowedSet_ImportsPendingUnclassifiedTicket(string? category, string? priority)
    {
        var ticket = Ticket.Import(Seed(category, priority, suggestedReply: null));

        Assert.Multiple(() =>
        {
            Assert.That(ticket.Status, Is.EqualTo(TicketStatus.Pending));
            Assert.That(ticket.Category, Is.Null);
            Assert.That(ticket.Priority, Is.Null);
            Assert.That(ticket.NextAttemptAt, Is.EqualTo(CreatedAt));
        });
    }

    [Test]
    public void Import_SuggestedReplyWithoutValidClassification_KeepsReply()
    {
        var ticket = Ticket.Import(Seed("Shipping", "Urgent", "Hi Adam, sorry about that. We will send a return label today."));

        Assert.That(ticket.SuggestedReply, Does.StartWith("Hi Adam"));
    }

    // Seed ticket t-009.
    [Test]
    public void Import_BodyWithCardData_MasksIt()
    {
        var import = Seed(null, null, null) with { Body = "My new card is 4111 1111 1111 1111, exp 09/29, CVV 123. Please put it on my account." };

        var ticket = Ticket.Import(import);

        Assert.That(ticket.Body, Is.EqualTo("My new card is [card ending 1111], exp 09/29, CVV [removed]. Please put it on my account."));
    }

    [Test]
    public void Import_LocalCreationTime_ThrowsValueOutOfRangeException()
    {
        var import = Seed(null, null, null) with { CreatedAt = new DateTime(2026, 10, 28, 9, 30, 0, DateTimeKind.Local) };

        Assert.Throws<ValueOutOfRangeException>(() => Ticket.Import(import));
    }

    [Test]
    public void Import_MissingEmail_ThrowsInvalidTicketException()
    {
        var import = Seed(null, null, null) with { CustomerEmail = string.Empty };

        Assert.Throws<InvalidTicketException>(() => Ticket.Import(import));
    }

    [Test]
    public void Constructor_BodyWithCardNumber_StoresMaskedBody()
    {
        var ticket = TicketFixture.Create(new FakeDateTime(), body: "Card 5555-5555-5555-4444 was charged twice.");

        Assert.That(ticket.Body, Is.EqualTo("Card [card ending 4444] was charged twice."));
    }

    [Test]
    public void Restore_Snapshot_RebuildsEveryField()
    {
        var updatedAt = CreatedAt.AddMinutes(3);
        var leaseExpiresAt = CreatedAt.AddMinutes(8);
        var snapshot = new TicketSnapshot(
            "t-002", "contoso", "mark.t@example.com", "App crashes on login", "Since the last update the app closes.",
            TicketStatus.Processing, ScreeningVerdict.Safe, "openai:m:screening-v1", updatedAt, TicketCategory.Technical, TicketPriority.High, "openai:m:classification-v1", updatedAt,
            null, 1, "earlier failure", TriageFailureReason.ProviderUnavailable, null, CreatedAt.AddMinutes(1), "worker-a", leaseExpiresAt, null,
            CreatedAt, updatedAt, false, null, "\"etag-1\"");

        var ticket = Ticket.Restore(snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(ticket.Id, Is.EqualTo("t-002"));
            Assert.That(ticket.TenantId, Is.EqualTo("contoso"));
            Assert.That(ticket.Status, Is.EqualTo(TicketStatus.Processing));
            Assert.That(ticket.Category, Is.EqualTo(TicketCategory.Technical));
            Assert.That(ticket.ScreeningVerdict, Is.EqualTo(ScreeningVerdict.Safe));
            Assert.That(ticket.FailedTriageAttempts, Is.EqualTo(1));
            Assert.That(ticket.TriageFailureReason, Is.EqualTo(TriageFailureReason.ProviderUnavailable));
            Assert.That(ticket.LeaseOwner, Is.EqualTo("worker-a"));
            Assert.That(ticket.LeaseExpiresAt, Is.EqualTo(leaseExpiresAt));
            Assert.That(ticket.CreatedAt, Is.EqualTo(CreatedAt));
            Assert.That(ticket.UpdatedAt, Is.EqualTo(updatedAt));
            Assert.That(ticket.Version, Is.EqualTo("\"etag-1\""));
            Assert.That(ticket.IsDeleted, Is.False);
        });
    }

    [Test]
    public void Restore_BlankVersion_ThrowsValueIsRequiredException()
    {
        var snapshot = new TicketSnapshot(
            "t-002", "contoso", "mark.t@example.com", "s", "b", TicketStatus.Pending, null, null, null, null, null, null, null, null, 0, null, null, null,
            CreatedAt, null, null, null, CreatedAt, null, false, null, " ");

        Assert.Throws<ValueIsRequiredException>(() => Ticket.Restore(snapshot));
    }

    private static TicketImport Seed(string? category, string? priority, string? suggestedReply)
    {
        return new TicketImport("t-004", "contoso", "lisa.m@example.com", "Change email address", "How can I change the email address on my account?", CreatedAt, category, priority, suggestedReply);
    }
}
