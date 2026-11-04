using SupportTriage.Application.Tickets.Dto;

namespace SupportTriage.Application.Tickets.Interfaces;

// Second layer of prompt-injection screening: a dedicated model call that judges whether the ticket tries to instruct
// or manipulate an AI. Throws InvalidTriageResponseException, TriageProviderUnavailableException or
// TriageProviderConfigurationException like the classifier; a ticket that could not be screened is never classified.
public interface IPromptInjectionScreener
{
    ValueTask<PromptInjectionScreeningDto> ScreenAsync(string subject, string body, CancellationToken cancellationToken = default);
}
