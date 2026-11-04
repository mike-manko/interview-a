using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Application.Tickets.Constants;

// Codes 2200-2299 belong to the ticket feature in the Application layer (errors.md).
internal static class TicketApplicationErrorMessages
{
    public static readonly ErrorMessage TicketContentIsRequired = new("A ticket needs a subject or a body.", 2201);

    public static readonly ErrorMessage TicketIdAlreadyExists = new("Ticket {0} already exists.", 2202, ErrorType.Conflict);

    public static readonly ErrorMessage CategoryFilterIsNotAllowed = new("Category filter '{0}' is not one of: {1}.", 2203);

    public static readonly ErrorMessage PriorityFilterIsNotAllowed = new("Priority filter '{0}' is not one of: {1}.", 2204);
}
