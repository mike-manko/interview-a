using Microsoft.Extensions.Logging;
using SupportTriage.Application.Tickets.Enums;
using SupportTriage.Application.Tickets.Interfaces;
using SupportTriage.Application.Tickets.Logging;
using SupportTriage.Domain.SharedKernel;
using SupportTriage.Domain.TicketAggregate;
using SupportTriage.Domain.TicketAggregate.ValueObjects;

namespace SupportTriage.Application.Tickets.ApplicationServices;

// Safe to run again: a ticket that is already classified, hidden or removed is left alone and the classifier is not
// asked. A ticket without content is classified by a fixed rule, also without asking the classifier.
internal sealed class TicketClassificationStep : ITicketClassificationStep
{
    private readonly ITicketClassifier _ticketClassifier;

    private readonly IDateTime _dateTime;

    private readonly ILogger<TicketClassificationStep> _logger;

    public TicketClassificationStep(ITicketClassifier ticketClassifier, IDateTime dateTime, ILogger<TicketClassificationStep> logger)
    {
        _ticketClassifier = ticketClassifier ?? throw new ArgumentNullException(nameof(ticketClassifier));
        _dateTime = dateTime ?? throw new ArgumentNullException(nameof(dateTime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<ClassifyTicketOutcome> ExecuteAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        if (ticket.IsDeleted)
        {
            TicketTriageLog.RemovedTicketSkipped(_logger, ticket.Id, ticket.TenantId);
            return ClassifyTicketOutcome.Removed;
        }

        if (ticket.IsHidden)
        {
            TicketTriageLog.HiddenTicketSkipped(_logger, ticket.Id, ticket.TenantId);
            return ClassifyTicketOutcome.Hidden;
        }

        if (ticket.IsClassified)
        {
            TicketTriageLog.TicketAlreadyClassified(_logger, ticket.Id, ticket.TenantId);
            return ClassifyTicketOutcome.AlreadyClassified;
        }

        TicketClassification classification;
        ClassifyTicketOutcome outcome;
        if (ticket.HasContent)
        {
            var result = await _ticketClassifier.ClassifyAsync(ticket.Subject, ticket.Body, cancellationToken);
            classification = new TicketClassification(result.Category, result.Priority, result.Source);
            outcome = ClassifyTicketOutcome.Classified;
        }
        else
        {
            classification = TicketClassification.ForEmptyTicket;
            outcome = ClassifyTicketOutcome.ClassifiedByRule;
        }

        if (!ticket.RecordClassification(classification, _dateTime))
        {
            TicketTriageLog.TicketAlreadyClassified(_logger, ticket.Id, ticket.TenantId);
            return ClassifyTicketOutcome.AlreadyClassified;
        }

        TicketTriageLog.TicketClassified(_logger, ticket.Id, ticket.TenantId, classification.Category, classification.Priority, classification.Source);

        return outcome;
    }
}
