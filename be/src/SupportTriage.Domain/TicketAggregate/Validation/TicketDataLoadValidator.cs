using System.Diagnostics.CodeAnalysis;
using SupportTriage.Domain.SharedKernel;
using SupportTriage.Domain.TicketAggregate.Constants;
using SupportTriage.Domain.TicketAggregate.Exceptions;

namespace SupportTriage.Domain.TicketAggregate.Validation;

internal sealed class TicketDataLoadValidator : ITicketDataLoadValidator
{
    public void ValidateTicketLoad<TTicket>([NotNull] TTicket? ticket, string ticketId)
        where TTicket : class
    {
        Guard.EnsureIsNotNullOrWhiteSpace(ticketId, TicketErrorMessages.TicketIdIsRequired);

        if (ticket is null)
            throw new TicketNotFoundException(TicketErrorMessages.TicketWithIdNotFound.WithMessageFormat(ticketId).WithKey(ticketId));
    }
}
