using SupportTriage.Domain.TicketAggregate.Enums;

namespace SupportTriage.Application.Tickets.Dto;

public sealed record TicketClassificationDto(TicketCategory Category, TicketPriority Priority, string Source);
