using NSubstitute;
using SupportTriage.Application.Common.Dto;
using SupportTriage.Application.Tickets.Constants;
using SupportTriage.Application.Tickets.Dto;
using SupportTriage.Application.Tickets.Exceptions;
using SupportTriage.Application.Tickets.Interfaces;
using SupportTriage.Application.Tickets.Queries;
using SupportTriage.Domain.TicketAggregate.Enums;

namespace SupportTriage.Application.Tests.Tickets.Queries;

[TestFixture]
public sealed class GetTicketsQueryHandlerTests
{
    private ITicketQuerySource _ticketQuerySource = null!;

    private GetTicketsQueryHandler _sut = null!;

    private TicketListFilter? _filter;

    private int _pageSize;

    [SetUp]
    public void SetUp()
    {
        _filter = null;
        _ticketQuerySource = Substitute.For<ITicketQuerySource>();
        _ticketQuerySource.GetTicketsAsync(default, default, default!, default).ReturnsForAnyArgs(call =>
        {
            _pageSize = call.ArgAt<int>(0);
            _filter = call.ArgAt<TicketListFilter>(2);
            return Task.FromResult(new ContinuationPageDto<TicketDto>([], null));
        });

        _sut = new GetTicketsQueryHandler(_ticketQuerySource);
    }

    [Test]
    public async Task Handle_NoFilters_ListsVisibleTicketsOfAnyClassification()
    {
        await _sut.Handle(new GetTicketsQuery(null, null, false, null, null), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(_filter!.IncludeHidden, Is.False);
            Assert.That(_filter.Categories, Is.Empty);
            Assert.That(_filter.Priorities, Is.Empty);
            Assert.That(_pageSize, Is.EqualTo(TicketPaging.DefaultPageSize));
        });
    }

    [Test]
    public async Task Handle_CategoryPriorityAndHiddenFilters_PassesThemTogether()
    {
        await _sut.Handle(new GetTicketsQuery(10, null, true, ["Billing", "Technical"], ["High"]), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(_filter!.IncludeHidden, Is.True);
            Assert.That(_filter.Categories, Is.EquivalentTo(new[] { TicketCategory.Billing, TicketCategory.Technical }));
            Assert.That(_filter.Priorities, Is.EquivalentTo(new[] { TicketPriority.High }));
        });
    }

    [Test]
    public async Task Handle_RepeatedValue_FiltersOnItOnce()
    {
        await _sut.Handle(new GetTicketsQuery(null, null, false, ["Billing", "Billing"], null), CancellationToken.None);

        Assert.That(_filter!.Categories, Has.Count.EqualTo(1));
    }

    // Filters accept the same exact names as classification: "tech", "billing" and "3" are not categories.
    [TestCase("tech")]
    [TestCase("billing")]
    [TestCase("3")]
    public void Handle_CategoryOutsideAllowedSet_ThrowsInvalidTicketFilterException(string category)
    {
        var exception = Assert.ThrowsAsync<InvalidTicketFilterException>(async () => await _sut.Handle(new GetTicketsQuery(null, null, false, [category], null), CancellationToken.None));

        Assert.That(exception!.Errors[0].Code, Is.EqualTo(2203));
    }

    [Test]
    public void Handle_InvalidCategoryAndPriority_ReportsBothAtOnce()
    {
        var exception = Assert.ThrowsAsync<InvalidTicketFilterException>(async () => await _sut.Handle(new GetTicketsQuery(null, null, false, ["tech"], ["Urgent"]), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Errors, Has.Count.EqualTo(2));
            Assert.That(exception.Errors[1].Code, Is.EqualTo(2204));
            Assert.That(exception.Errors[1].Message, Does.Contain("'Urgent'").And.Contain("Low, Medium, High, Critical"));
        });
    }

    [Test]
    public async Task Handle_InvalidFilter_DoesNotQueryTheStore()
    {
        Assert.ThrowsAsync<InvalidTicketFilterException>(async () => await _sut.Handle(new GetTicketsQuery(null, null, false, ["tech"], null), CancellationToken.None));

        await _ticketQuerySource.DidNotReceiveWithAnyArgs().GetTicketsAsync(default, default, default!, default);
    }

    [TestCase(0, 1)]
    [TestCase(500, TicketPaging.MaxPageSize)]
    [TestCase(42, 42)]
    public async Task Handle_PageSize_IsClamped(int requested, int expected)
    {
        await _sut.Handle(new GetTicketsQuery(requested, null, false, null, null), CancellationToken.None);

        Assert.That(_pageSize, Is.EqualTo(expected));
    }
}
