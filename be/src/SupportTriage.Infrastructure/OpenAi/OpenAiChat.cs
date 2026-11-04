using System.ClientModel;
using System.ClientModel.Primitives;
using OpenAI;
using OpenAI.Chat;
using Polly.Timeout;
using SupportTriage.Application.Tickets.Exceptions;
using SupportTriage.Infrastructure.OpenAi.Constants;
using SupportTriage.Infrastructure.OpenAi.Settings;

namespace SupportTriage.Infrastructure.OpenAi;

// Shared by every OpenAI adapter: one way to build the client (the SDK's own retries off, the resilience pipeline on
// the HttpClient does them) and one translation of transport failures into Application exceptions.
internal static class OpenAiChat
{
    private const int NoSdkRetries = 0;

    // ClientResultException.Status is 0 when the request never got an HTTP response.
    private const int NoHttpResponseStatus = 0;

    private const int UnauthorizedStatus = 401;

    private const int ForbiddenStatus = 403;

    private const int NotFoundStatus = 404;

    public static ChatClient CreateClient(HttpClient httpClient, OpenAiSettings settings, string model)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        var options = new OpenAIClientOptions
        {
            Transport = new HttpClientPipelineTransport(httpClient),
            RetryPolicy = new ClientRetryPolicy(NoSdkRetries),
            NetworkTimeout = settings.TotalTimeout,
        };
        if (settings.Endpoint is not null)
            options.Endpoint = settings.Endpoint;

        return new ChatClient(model, new ApiKeyCredential(settings.ApiKey), options);
    }

    public static async Task<ChatCompletion> CompleteAsync(ChatClient chatClient, IEnumerable<ChatMessage> messages, ChatCompletionOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chatClient);

        try
        {
            return (await chatClient.CompleteChatAsync(messages, options, cancellationToken)).Value;
        }
        catch (ClientResultException ex) when (ex.Status is UnauthorizedStatus or ForbiddenStatus or NotFoundStatus)
        {
            throw new TriageProviderConfigurationException(OpenAiErrorMessages.ProviderRejectedConfiguration.WithMessageFormat(ex.Status), ex);
        }
        catch (ClientResultException ex) when (ex.Status != NoHttpResponseStatus)
        {
            throw new TriageProviderUnavailableException(OpenAiErrorMessages.ProviderReturnedError.WithMessageFormat(ex.Status), ex);
        }
        catch (ClientResultException ex)
        {
            throw new TriageProviderUnavailableException(OpenAiErrorMessages.ProviderUnreachable, ex);
        }
        catch (HttpRequestException ex)
        {
            throw new TriageProviderUnavailableException(OpenAiErrorMessages.ProviderUnreachable, ex);
        }
        catch (TimeoutRejectedException ex)
        {
            throw new TriageProviderUnavailableException(OpenAiErrorMessages.ProviderTimedOut, ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TriageProviderUnavailableException(OpenAiErrorMessages.ProviderTimedOut, ex);
        }
    }

    public static string? FirstText(ChatCompletion completion)
    {
        ArgumentNullException.ThrowIfNull(completion);

        return completion.Content.Count > 0 ? completion.Content[0].Text : null;
    }
}
