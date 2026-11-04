using SupportTriage.Domain.SharedKernel;
using SupportTriage.Domain.TicketAggregate.Constants;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.Exceptions;

namespace SupportTriage.Domain.TicketAggregate.ValueObjects;

// One classification step: category and priority are decided together, and Source records what decided them
// (model and prompt version, or a rule), so a prompt change can be traced back to the tickets it classified.
public sealed record TicketClassification
{
    public const int MaxSourceLength = 200;

    public const string EmptyTicketRuleSource = "rule:empty-ticket";

    public TicketCategory Category { get; }

    public TicketPriority Priority { get; }

    public string Source { get; }

    // A ticket without subject and body has nothing to classify, so no model is asked.
    public static readonly TicketClassification ForEmptyTicket = new(TicketCategory.Other, TicketPriority.Low, EmptyTicketRuleSource);

    private TicketClassification()
    {
        Source = string.Empty;
    }

    public TicketClassification(TicketCategory category, TicketPriority priority, string source)
    {
        List<ErrorMessage>? errors = null;
        if (!Enum.IsDefined(category))
            (errors ??= []).Add(TicketErrorMessages.CategoryIsNotAllowed.WithMessageFormat((int)category).WithKey(nameof(category)));

        if (!Enum.IsDefined(priority))
            (errors ??= []).Add(TicketErrorMessages.PriorityIsNotAllowed.WithMessageFormat((int)priority).WithKey(nameof(priority)));

        if (string.IsNullOrWhiteSpace(source))
            (errors ??= []).Add(TicketErrorMessages.ClassificationSourceIsRequired.WithKey(nameof(source)));
        else if (source.Length > MaxSourceLength)
            (errors ??= []).Add(TicketErrorMessages.ClassificationSourceIsTooLong.WithMessageFormat(MaxSourceLength).WithKey(nameof(source)));

        if (errors is not null)
            throw new InvalidTicketClassificationException(errors);

        Category = category;
        Priority = priority;
        Source = source;
    }
}
