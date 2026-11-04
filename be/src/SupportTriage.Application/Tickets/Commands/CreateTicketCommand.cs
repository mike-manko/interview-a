using Mediator;
using Microsoft.Extensions.Logging;
using SupportTriage.Application.Common.Interfaces;
using SupportTriage.Application.Tickets.Constants;
using SupportTriage.Application.Tickets.Dto;
using SupportTriage.Application.Tickets.Exceptions;
using SupportTriage.Application.Tickets.Logging;
using SupportTriage.Domain.SharedKernel;
using SupportTriage.Domain.TicketAggregate;
using SupportTriage.Domain.TicketAggregate.Exceptions;
using SupportTriage.Domain.TicketAggregate.Interfaces.Repositories;

namespace SupportTriage.Application.Tickets.Commands;

// Stores the ticket as Pending and returns at once; the triage worker classifies it later.
public sealed record CreateTicketCommand(string CustomerEmail, string? Subject, string? Body) : IRequest<TicketCreatedDto>;

public sealed class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, TicketCreatedDto>
{
    private readonly ITicketRepository _ticketRepository;

    private readonly ITenantScope _tenantScope;

    private readonly IIdGenerator<string> _idGenerator;

    private readonly IDateTime _dateTime;

    private readonly ILogger<CreateTicketCommandHandler> _logger;

    public CreateTicketCommandHandler(ITicketRepository ticketRepository, ITenantScope tenantScope, IIdGenerator<string> idGenerator, IDateTime dateTime, ILogger<CreateTicketCommandHandler> logger)
    {
        _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
        _tenantScope = tenantScope ?? throw new ArgumentNullException(nameof(tenantScope));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        _dateTime = dateTime ?? throw new ArgumentNullException(nameof(dateTime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<TicketCreatedDto> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Subject) && string.IsNullOrWhiteSpace(request.Body))
            throw new InvalidTicketException(TicketApplicationErrorMessages.TicketContentIsRequired);

        var tenantId = _tenantScope.RequireTenantId();
        var ticket = new Ticket(_idGenerator, tenantId, request.CustomerEmail?.Trim()!, request.Subject?.Trim() ?? string.Empty, request.Body?.Trim() ?? string.Empty, _dateTime);

        if (!await _ticketRepository.TryAddAsync(ticket, cancellationToken))
            throw new TicketAlreadyExistsException(TicketApplicationErrorMessages.TicketIdAlreadyExists.WithMessageFormat(ticket.Id).WithKey(ticket.Id));

        TicketTriageLog.TicketCreated(_logger, ticket.Id, tenantId);

        return new TicketCreatedDto(ticket.Id, ticket.Status);
    }
}
