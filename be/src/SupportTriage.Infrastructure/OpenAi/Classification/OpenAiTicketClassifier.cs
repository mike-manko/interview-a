using System.Globalization;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using SupportTriage.Application.Tickets.Dto;
using SupportTriage.Application.Tickets.Interfaces;
using SupportTriage.Infrastructure.OpenAi.Settings;

namespace SupportTriage.Infrastructure.OpenAi.Classification;

// Typed HTTP client: the HttpClient comes from IHttpClientFactory with the resilience pipeline attached.
internal sealed class OpenAiTicketClassifier : ITicketClassifier
{
    private readonly ChatClient _chatClient;

    private readonly float? _temperature;

    private readonly int _maxOutputTokens;

    private readonly string _source;

    public OpenAiTicketClassifier(HttpClient httpClient, IOptions<OpenAiSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(settings);

        var value = settings.Value;
        _chatClient = OpenAiChat.CreateClient(httpClient, value, value.ClassificationModel);
        _temperature = value.ClassificationTemperature;
        _maxOutputTokens = value.ClassificationMaxOutputTokens;
        _source = string.Create(CultureInfo.InvariantCulture, $"openai:{value.ClassificationModel}:{TicketClassificationPrompt.Version}");
    }

    public async ValueTask<TicketClassificationDto> ClassifyAsync(string subject, string body, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(body);

        ChatMessage[] messages =
        [
            new SystemChatMessage(TicketClassificationPrompt.SystemMessage),
            new UserChatMessage(TicketClassificationPrompt.BuildUserMessage(subject, body)),
        ];

        // A new options object per call: the SDK writes the messages and model into the instance it is given.
        var options = new ChatCompletionOptions
        {
            ResponseFormat = TicketClassificationSchema.ResponseFormat,
            Temperature = _temperature,
            MaxOutputTokenCount = _maxOutputTokens,
        };

        var completion = await OpenAiChat.CompleteAsync(_chatClient, messages, options, cancellationToken);

        return TicketClassificationResponseParser.Parse(OpenAiChat.FirstText(completion), completion.Refusal, completion.FinishReason, _source);
    }
}
