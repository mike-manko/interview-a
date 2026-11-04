using System.Diagnostics.CodeAnalysis;

namespace SupportTriage.Domain.TicketAggregate.Validation;

public interface ITicketDataLoadValidator
{
    void ValidateTicketLoad<TTicket>([NotNull] TTicket? ticket, string ticketId)
        where TTicket : class;
}
