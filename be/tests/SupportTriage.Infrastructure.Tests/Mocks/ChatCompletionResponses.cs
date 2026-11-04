using System.Net;
using System.Text;
using System.Text.Json;

namespace SupportTriage.Infrastructure.Tests.Mocks;

// Chat Completions wire format, as the OpenAI API returns it.
internal static class ChatCompletionResponses
{
    private const string JsonMediaType = "application/json";

    public static HttpResponseMessage Classification(string category, string priority)
    {
        return Completion(JsonSerializer.Serialize(new { category, priority }));
    }

    public static HttpResponseMessage Completion(string? content, string? refusal = null, string finishReason = "stop")
    {
        var body = JsonSerializer.Serialize(new
        {
            id = "chatcmpl-test",
            @object = "chat.completion",
            created = 1_761_638_400,
            model = "test-model",
            choices = new[]
            {
                new
                {
                    index = 0,
                    message = new { role = "assistant", content, refusal },
                    finish_reason = finishReason,
                },
            },
            usage = new { prompt_tokens = 120, completion_tokens = 12, total_tokens = 132 },
        });

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, JsonMediaType) };
    }

    public static HttpResponseMessage Error(HttpStatusCode status)
    {
        const string body = """{"error":{"message":"stub failure","type":"server_error","param":null,"code":null}}""";

        return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, JsonMediaType) };
    }
}
