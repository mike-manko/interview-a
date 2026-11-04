using SupportTriage.Application.Tickets.Dto;

namespace SupportTriage.Application.Tickets.Interfaces;

// Decides category and priority for one ticket in one call. Implementations return only allowed values; they throw
// InvalidTriageResponseException when the provider answers with something else, TriageProviderUnavailableException
// when the provider cannot be reached after retries, and TriageProviderConfigurationException when it rejects our
// credentials or model (which must not count against the ticket).
public interface ITicketClassifier
{
    ValueTask<TicketClassificationDto> ClassifyAsync(string subject, string body, CancellationToken cancellationToken = default);
}
