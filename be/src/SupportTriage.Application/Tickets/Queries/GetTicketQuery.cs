using Mediator;
using SupportTriage.Application.Tickets.Dto;
using SupportTriage.Application.Tickets.Interfaces;
using SupportTriage.Domain.TicketAggregate.Validation;

namespace SupportTriage.Application.Tickets.Queries;

// One ticket of the current tenant. A hidden ticket is reported as not found unless IncludeHidden is true.
public sealed record GetTicketQuery(string TicketId, bool IncludeHidden) : IRequest<TicketDto>;

public sealed class GetTicketQueryHandler : IRequestHandler<GetTicketQuery, TicketDto>
{
    private readonly ITicketQuerySource _ticketQuerySource;

    private readonly ITicketDataLoadValidator _ticketDataLoadValidator;

    public GetTicketQueryHandler(ITicketQuerySource ticketQuerySource, ITicketDataLoadValidator ticketDataLoadValidator)
    {
        _ticketQuerySource = ticketQuerySource ?? throw new ArgumentNullException(nameof(ticketQuerySource));
        _ticketDataLoadValidator = ticketDataLoadValidator ?? throw new ArgumentNullException(nameof(ticketDataLoadValidator));
    }

    public async ValueTask<TicketDto> Handle(GetTicketQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketQuerySource.GetTicketAsync(request.TicketId, request.IncludeHidden, cancellationToken);
        _ticketDataLoadValidator.ValidateTicketLoad(ticket, request.TicketId);

        return ticket;
    }
}
