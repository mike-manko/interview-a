using SupportTriage.Domain.TicketAggregate.Enums;

namespace SupportTriage.Application.Tickets.Dto;

public sealed record TicketCreatedDto(string Id, TicketStatus Status);
