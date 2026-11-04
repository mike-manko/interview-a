using SupportTriage.Application.Tickets.Constants;
using SupportTriage.Domain.SharedKernel;
using SupportTriage.Domain.TicketAggregate;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.ValueObjects;
using SupportTriage.Persistence.Documents;
using SupportTriage.Persistence.Mapping;

namespace SupportTriage.Persistence.Tests.Mapping;

[TestFixture]
public sealed class TicketDocumentMapperTests
{
    private const string WorkerId = "worker-a";

    private static readonly IDateTime Clock = new FrozenDateTime(new DateTime(2026, 10, 28, 8, 0, 0, DateTimeKind.Utc));

    [Test]
    public void ToTicket_DocumentFromToDocument_RestoresTheSameState()
    {
        var ticket = new Ticket(new FixedIdGenerator("t-001"), "contoso", "anna.k@example.com", "Charged twice", "Two charges of 49 EUR.", Clock);
        ticket.TryClaim(WorkerId, TimeSpan.FromMinutes(5), Clock);
        ticket.RecordTriageFailure(TriageFailureReason.ProviderUnavailable, "HTTP 503", 3, TimeSpan.FromMinutes(1), Clock);

        var restored = TicketDocumentMapper.ToTicket(TicketDocumentMapper.ToDocument(ticket), "\"etag-2\"");

        Assert.Multiple(() =>
        {
            Assert.That(restored.Id, Is.EqualTo(ticket.Id));
            Assert.That(restored.TenantId, Is.EqualTo(ticket.TenantId));
            Assert.That(restored.Status, Is.EqualTo(ticket.Status));
            Assert.That(restored.FailedTriageAttempts, Is.EqualTo(1));
            Assert.That(restored.TriageFailureReason, Is.EqualTo(TriageFailureReason.ProviderUnavailable));
            Assert.That(restored.TriageFailureNote, Is.EqualTo("HTTP 503"));
            Assert.That(restored.NextAttemptAt, Is.EqualTo(ticket.NextAttemptAt));
            Assert.That(restored.CreatedAt, Is.EqualTo(ticket.CreatedAt));
            Assert.That(restored.UpdatedAt, Is.EqualTo(ticket.UpdatedAt));
            Assert.That(restored.Version, Is.EqualTo("\"etag-2\""));
        });
    }

    [Test]
    public void ToDto_HiddenTicket_IsHiddenWithStopMessage()
    {
        var document = Document(TicketStatus.Failed, TriageFailureReason.InvalidResponse);

        var dto = TicketDocumentMapper.ToDto(document);

        Assert.Multiple(() =>
        {
            Assert.That(dto.IsHidden, Is.True);
            Assert.That(dto.FailureReason, Is.EqualTo(TriageFailureReason.InvalidResponse));
            Assert.That(dto.FailureMessage, Is.EqualTo(TriageFailureMessages.For(TriageFailureReason.InvalidResponse, isHidden: true)));
            Assert.That(dto.FailedTriageAttempts, Is.EqualTo(2));
        });
    }

    [Test]
    public void ToDto_PendingTicketAfterFailedAttempt_ShowsRetryMessage()
    {
        var dto = TicketDocumentMapper.ToDto(Document(TicketStatus.Pending, TriageFailureReason.ProviderUnavailable));

        Assert.Multiple(() =>
        {
            Assert.That(dto.IsHidden, Is.False);
            Assert.That(dto.FailureMessage, Is.EqualTo(TriageFailureMessages.For(TriageFailureReason.ProviderUnavailable, isHidden: false)));
        });
    }

    [Test]
    public void ToDto_CompletedTicket_HasNoFailureMessage()
    {
        var dto = TicketDocumentMapper.ToDto(Document(TicketStatus.Completed, TriageFailureReason.ProviderUnavailable));

        Assert.Multiple(() =>
        {
            Assert.That(dto.FailureReason, Is.Null);
            Assert.That(dto.FailureMessage, Is.Null);
        });
    }

    [Test]
    public void ToDto_NeverFailed_HasNoFailureMessage()
    {
        var dto = TicketDocumentMapper.ToDto(Document(TicketStatus.Pending, reason: null));

        Assert.That(dto.FailureMessage, Is.Null);
    }

    [Test]
    public void ToDocument_ClassifiedTicket_CopiesClassification()
    {
        var ticket = new Ticket(new FixedIdGenerator("t-002"), "contoso", "mark.t@example.com", "App crashes on login", "iPhone 15.", Clock);
        ticket.RecordScreening(new TicketScreening(ScreeningVerdict.Safe, "openai:m:screening-v1", null), Clock);
        ticket.RecordClassification(new TicketClassification(TicketCategory.Technical, TicketPriority.High, "openai:m:classification-v1"), Clock);

        var document = TicketDocumentMapper.ToDocument(ticket);

        Assert.Multiple(() =>
        {
            Assert.That(document.Category, Is.EqualTo(TicketCategory.Technical));
            Assert.That(document.Priority, Is.EqualTo(TicketPriority.High));
            Assert.That(document.ClassificationSource, Is.EqualTo("openai:m:classification-v1"));
            Assert.That(document.ETag, Is.Null);
        });
    }

    private static TicketDocument Document(TicketStatus status, TriageFailureReason? reason)
    {
        return new TicketDocument
        {
            Id = "t-011",
            TenantId = "contoso",
            CustomerEmail = "nina.r@example.com",
            Subject = "Thanks!",
            Body = "Great support.",
            Status = status,
            FailedTriageAttempts = 2,
            TriageFailureReason = reason,
            CreatedAt = Clock.UtcNow,
            NextAttemptAt = Clock.UtcNow,
        };
    }

    private sealed class FixedIdGenerator : IIdGenerator<string>
    {
        private readonly string _id;

        public FixedIdGenerator(string id)
        {
            _id = id;
        }

        public string NewId() => _id;

        public ValueTask<string> NewIdAsync(CancellationToken cancellationToken = default) => new(_id);
    }
}
