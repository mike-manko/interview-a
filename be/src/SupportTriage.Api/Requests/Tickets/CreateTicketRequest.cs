using System.ComponentModel.DataAnnotations;
using SupportTriage.Domain.TicketAggregate;

namespace SupportTriage.Api.Requests.Tickets;

public sealed class CreateTicketRequest : IValidatableObject
{
    [Required]
    [EmailAddress]
    [MaxLength(Ticket.MaxCustomerEmailLength)]
    public string CustomerEmail { get; set; } = null!;

    [MaxLength(Ticket.MaxSubjectLength)]
    public string? Subject { get; set; }

    [MaxLength(Ticket.MaxBodyLength)]
    public string? Body { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Subject) && string.IsNullOrWhiteSpace(Body))
            yield return new ValidationResult("Provide a subject or a body.", [nameof(Subject), nameof(Body)]);
    }
}
