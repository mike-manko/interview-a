using Mediator;
using SupportTriage.Application.Tickets.Dto;
using SupportTriage.Application.Tickets.Interfaces;
using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Application.Tickets.Queries;

// Tickets of every tenant that are due for triage: pending and past their next attempt time, or processing with an
// expired lease (the worker that held it crashed).
public sealed record GetDueTicketsQuery(int MaxCount) : IRequest<IReadOnlyList<TicketWorkItemDto>>;

public sealed class GetDueTicketsQueryHandler : IRequestHandler<GetDueTicketsQuery, IReadOnlyList<TicketWorkItemDto>>
{
    private readonly ITicketWorkSource _ticketWorkSource;

    private readonly IDateTime _dateTime;

    public GetDueTicketsQueryHandler(ITicketWorkSource ticketWorkSource, IDateTime dateTime)
    {
        _ticketWorkSource = ticketWorkSource ?? throw new ArgumentNullException(nameof(ticketWorkSource));
        _dateTime = dateTime ?? throw new ArgumentNullException(nameof(dateTime));
    }

    public async ValueTask<IReadOnlyList<TicketWorkItemDto>> Handle(GetDueTicketsQuery request, CancellationToken cancellationToken)
    {
        return await _ticketWorkSource.GetDueTicketsAsync(request.MaxCount, _dateTime.UtcNow, cancellationToken);
    }
}
