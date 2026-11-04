using System.Globalization;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using SupportTriage.Application.Tickets.Dto;
using SupportTriage.Application.Tickets.Interfaces;
using SupportTriage.Infrastructure.OpenAi.Classification;
using SupportTriage.Infrastructure.OpenAi.Settings;

namespace SupportTriage.Infrastructure.OpenAi.Screening;

// A separate call with its own prompt, so the screening model never sees the classification instructions it is
// protecting and a manipulated classification prompt cannot also switch off the screening.
internal sealed class OpenAiPromptInjectionScreener : IPromptInjectionScreener
{
    private const float ScreeningTemperature = 0f;

    private readonly ChatClient _chatClient;

    private readonly int _maxOutputTokens;

    private readonly bool _sendTemperature;

    private readonly string _source;

    public OpenAiPromptInjectionScreener(HttpClient httpClient, IOptions<OpenAiSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(settings);

        var value = settings.Value;
        var model = string.IsNullOrWhiteSpace(value.ScreeningModel) ? value.ClassificationModel : value.ScreeningModel;
        _chatClient = OpenAiChat.CreateClient(httpClient, value, model);
        _maxOutputTokens = value.ScreeningMaxOutputTokens;
        _sendTemperature = value.ClassificationTemperature is not null;
        _source = string.Create(CultureInfo.InvariantCulture, $"openai:{model}:{PromptInjectionScreeningPrompt.Version}");
    }

    public async ValueTask<PromptInjectionScreeningDto> ScreenAsync(string subject, string body, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(body);

        ChatMessage[] messages =
        [
            new SystemChatMessage(PromptInjectionScreeningPrompt.SystemMessage),
            new UserChatMessage(TicketClassificationPrompt.BuildUserMessage(subject, body)),
        ];

        var options = new ChatCompletionOptions
        {
            ResponseFormat = PromptInjectionScreeningSchema.ResponseFormat,
            Temperature = _sendTemperature ? ScreeningTemperature : null,
            MaxOutputTokenCount = _maxOutputTokens,
        };

        var completion = await OpenAiChat.CompleteAsync(_chatClient, messages, options, cancellationToken);

        return PromptInjectionScreeningResponseParser.Parse(OpenAiChat.FirstText(completion), completion.Refusal, completion.FinishReason, _source);
    }
}
