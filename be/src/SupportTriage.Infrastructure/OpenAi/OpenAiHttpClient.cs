namespace SupportTriage.Infrastructure.OpenAi;

// One named client per typed client (IHttpClientFactory does not allow two typed clients on one name); both get the
// same resilience pipeline.
internal static class OpenAiHttpClient
{
    public const string ClassificationName = "openai-classification";

    public const string ScreeningName = "openai-screening";

    public const string ResiliencePipelineName = "openai-retry";
}
