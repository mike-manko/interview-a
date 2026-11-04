using SupportTriage.Domain.TicketAggregate.Enums;

namespace SupportTriage.Application.Tickets.Dto;

// Filters of the ticket list. An empty set means "any value". A ticket that is not classified yet has no category
// or priority, so any category or priority filter leaves it out.
public sealed record TicketListFilter(bool IncludeHidden, IReadOnlySet<TicketCategory> Categories, IReadOnlySet<TicketPriority> Priorities);
