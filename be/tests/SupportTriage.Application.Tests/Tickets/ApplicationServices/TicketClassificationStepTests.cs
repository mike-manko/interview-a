using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SupportTriage.Application.Tests.Fixtures;
using SupportTriage.Application.Tests.Mocks;
using SupportTriage.Application.Tickets.ApplicationServices;
using SupportTriage.Application.Tickets.Dto;
using SupportTriage.Application.Tickets.Enums;
using SupportTriage.Application.Tickets.Exceptions;
using SupportTriage.Application.Tickets.Interfaces;
using SupportTriage.Domain.SharedKernel;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.Exceptions;
using SupportTriage.Domain.TicketAggregate.ValueObjects;

namespace SupportTriage.Application.Tests.Tickets.ApplicationServices;

[TestFixture]
public sealed class TicketClassificationStepTests
{
    private const string Source = "openai:test-model:classification-v1";

    private static readonly TicketClassificationDto BillingHigh = new(TicketCategory.Billing, TicketPriority.High, Source);

    private ITicketClassifier _ticketClassifier = null!;

    private TicketClassificationStep _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _ticketClassifier = Substitute.For<ITicketClassifier>();
        _ticketClassifier.ClassifyAsync(default!, default!, default).ReturnsForAnyArgs(new ValueTask<TicketClassificationDto>(BillingHigh));

        _sut = new TicketClassificationStep(_ticketClassifier, new FakeDateTime(), NullLogger<TicketClassificationStep>.Instance);
    }

    [Test]
    public async Task ExecuteAsync_UnclassifiedTicket_RecordsClassifierResult()
    {
        var ticket = TicketFixture.CreateScreened();

        var outcome = await _sut.ExecuteAsync(ticket);

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.EqualTo(ClassifyTicketOutcome.Classified));
            Assert.That(ticket.Category, Is.EqualTo(TicketCategory.Billing));
            Assert.That(ticket.Priority, Is.EqualTo(TicketPriority.High));
            Assert.That(ticket.ClassificationSource, Is.EqualTo(Source));
        });
    }

    [Test]
    public async Task ExecuteAsync_UnclassifiedTicket_SendsSubjectAndBodyToClassifier()
    {
        using var cancellation = new CancellationTokenSource();

        await _sut.ExecuteAsync(TicketFixture.CreateScreened(), cancellation.Token);

        await _ticketClassifier.Received(1).ClassifyAsync(TicketFixture.Subject, TicketFixture.Body, cancellation.Token);
    }

    [Test]
    public async Task ExecuteAsync_SameTicketTwice_AsksClassifierOnce()
    {
        var ticket = TicketFixture.CreateScreened();

        var first = await _sut.ExecuteAsync(ticket);
        var second = await _sut.ExecuteAsync(ticket);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(ClassifyTicketOutcome.Classified));
            Assert.That(second, Is.EqualTo(ClassifyTicketOutcome.AlreadyClassified));
        });
        await _ticketClassifier.ReceivedWithAnyArgs(1).ClassifyAsync(default!, default!, default);
    }

    [Test]
    public async Task ExecuteAsync_AlreadyClassifiedTicket_SkipsClassifier()
    {
        var ticket = TicketFixture.CreateClassified();

        var outcome = await _sut.ExecuteAsync(ticket);

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.EqualTo(ClassifyTicketOutcome.AlreadyClassified));
            Assert.That(ticket.Category, Is.EqualTo(TicketCategory.Account));
        });
        await _ticketClassifier.DidNotReceiveWithAnyArgs().ClassifyAsync(default!, default!, default);
    }

    // Seed ticket t-007: empty subject and body.
    [Test]
    public async Task ExecuteAsync_TicketWithoutContent_ClassifiesByRuleWithoutClassifier()
    {
        var ticket = TicketFixture.CreateScreened(subject: string.Empty, body: string.Empty);

        var outcome = await _sut.ExecuteAsync(ticket);

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.EqualTo(ClassifyTicketOutcome.ClassifiedByRule));
            Assert.That(ticket.Category, Is.EqualTo(TicketCategory.Other));
            Assert.That(ticket.Priority, Is.EqualTo(TicketPriority.Low));
            Assert.That(ticket.ClassificationSource, Is.EqualTo(TicketClassification.EmptyTicketRuleSource));
        });
        await _ticketClassifier.DidNotReceiveWithAnyArgs().ClassifyAsync(default!, default!, default);
    }

    [Test]
    public async Task ExecuteAsync_HiddenTicket_SkipsClassifier()
    {
        var outcome = await _sut.ExecuteAsync(TicketFixture.CreateHidden());

        Assert.That(outcome, Is.EqualTo(ClassifyTicketOutcome.Hidden));
        await _ticketClassifier.DidNotReceiveWithAnyArgs().ClassifyAsync(default!, default!, default);
    }

    [Test]
    public async Task ExecuteAsync_RemovedTicket_SkipsClassifier()
    {
        var outcome = await _sut.ExecuteAsync(TicketFixture.CreateRemoved());

        Assert.That(outcome, Is.EqualTo(ClassifyTicketOutcome.Removed));
        await _ticketClassifier.DidNotReceiveWithAnyArgs().ClassifyAsync(default!, default!, default);
    }

    [Test]
    public void ExecuteAsync_ClassifierUnavailable_PropagatesAndLeavesTicketUnclassified()
    {
        var ticket = TicketFixture.CreateScreened();
        ArrangeClassifierFailure(new TriageProviderUnavailableException(new ErrorMessage("OpenAI answered with HTTP 503 after retries.", 3001, ErrorType.Unexpected)));

        Assert.ThrowsAsync<TriageProviderUnavailableException>(async () => await _sut.ExecuteAsync(ticket));

        Assert.That(ticket.IsClassified, Is.False);
    }

    [Test]
    public void ExecuteAsync_ClassifierRejectsResponse_PropagatesAndLeavesTicketUnclassified()
    {
        var ticket = TicketFixture.CreateScreened();
        ArrangeClassifierFailure(new InvalidTriageResponseException(new ErrorMessage("The model returned category 'tech'.", 3009, ErrorType.Unexpected)));

        Assert.ThrowsAsync<InvalidTriageResponseException>(async () => await _sut.ExecuteAsync(ticket));

        Assert.That(ticket.IsClassified, Is.False);
    }

    // Defence in depth: even if a classifier lets an undefined enum value through, it is never recorded.
    [Test]
    public void ExecuteAsync_ClassifierReturnsUndefinedCategory_ThrowsAndLeavesTicketUnclassified()
    {
        var ticket = TicketFixture.CreateScreened();
        _ticketClassifier.ClassifyAsync(default!, default!, default).ReturnsForAnyArgs(new ValueTask<TicketClassificationDto>(new TicketClassificationDto((TicketCategory)42, TicketPriority.High, Source)));

        Assert.ThrowsAsync<InvalidTicketClassificationException>(async () => await _sut.ExecuteAsync(ticket));

        Assert.That(ticket.IsClassified, Is.False);
    }

    private void ArrangeClassifierFailure(Exception exception)
    {
        _ticketClassifier.ClassifyAsync(default!, default!, default).ReturnsForAnyArgs(_ => ValueTask.FromException<TicketClassificationDto>(exception));
    }
}
