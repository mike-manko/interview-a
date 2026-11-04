using SupportTriage.Domain.SharedKernel.Exceptions;
using SupportTriage.Domain.Tests.Fixtures;
using SupportTriage.Domain.Tests.Mocks;
using SupportTriage.Domain.TicketAggregate;
using SupportTriage.Domain.TicketAggregate.Exceptions;
using SupportTriage.Domain.TicketAggregate.Validation;

namespace SupportTriage.Domain.Tests.TicketAggregate.Validation;

[TestFixture]
public sealed class TicketDataLoadValidatorTests
{
    private TicketDataLoadValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new TicketDataLoadValidator();
    }

    [Test]
    public void ValidateTicketLoad_LoadedTicket_DoesNotThrow()
    {
        var ticket = TicketFixture.Create(new FakeDateTime());

        Assert.DoesNotThrow(() => _sut.ValidateTicketLoad(ticket, TicketFixture.TicketId));
    }

    [Test]
    public void ValidateTicketLoad_MissingTicket_ThrowsTicketNotFoundException()
    {
        var exception = Assert.Throws<TicketNotFoundException>(() => _sut.ValidateTicketLoad<Ticket>(null, TicketFixture.TicketId));

        Assert.That(exception!.Errors[0].Key, Is.EqualTo(TicketFixture.TicketId));
    }

    [TestCase(null)]
    [TestCase("")]
    public void ValidateTicketLoad_BlankTicketId_ThrowsValueIsRequiredException(string? ticketId)
    {
        Assert.Throws<ValueIsRequiredException>(() => _sut.ValidateTicketLoad<Ticket>(null, ticketId!));
    }
}
