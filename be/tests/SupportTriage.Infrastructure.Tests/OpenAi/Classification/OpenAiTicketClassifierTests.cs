using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SupportTriage.Application.Tickets.Exceptions;
using SupportTriage.Application.Tickets.Interfaces;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.Services;
using SupportTriage.Infrastructure.OpenAi;
using SupportTriage.Infrastructure.OpenAi.Classification;
using SupportTriage.Infrastructure.OpenAi.Settings;
using SupportTriage.Infrastructure.Tests.Mocks;

namespace SupportTriage.Infrastructure.Tests.OpenAi.Classification;

// Runs the real DI registration, resilience pipeline and OpenAI SDK against a stub HTTP handler.
[TestFixture]
public sealed class OpenAiTicketClassifierTests
{
    private const string Model = "test-model";

    private const int MaxRetryAttempts = 3;

    private const string Subject = "Charged twice for October";

    private const string Body = "Hi, I see two charges of 49 EUR on my card for the October subscription. Please refund one of them.";

    private StubHttpMessageHandler _handler = null!;

    private ServiceProvider _provider = null!;

    private ITicketClassifier _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _handler = new StubHttpMessageHandler();
        _provider = BuildProvider(_handler, Settings());
        _sut = _provider.GetRequiredService<ITicketClassifier>();
    }

    [TearDown]
    public void TearDown()
    {
        _provider.Dispose();
        _handler.Dispose();
    }

    [Test]
    public async Task ClassifyAsync_AllowedValues_ReturnsClassificationWithModelAndPromptVersion()
    {
        _handler.Enqueue(static () => ChatCompletionResponses.Classification("Billing", "High"));

        var result = await _sut.ClassifyAsync(Subject, Body);

        Assert.Multiple(() =>
        {
            Assert.That(result.Category, Is.EqualTo(TicketCategory.Billing));
            Assert.That(result.Priority, Is.EqualTo(TicketPriority.High));
            Assert.That(result.Source, Is.EqualTo("openai:" + Model + ":" + TicketClassificationPrompt.Version));
            Assert.That(_handler.RequestCount, Is.EqualTo(1));
            Assert.That(_handler.RequestUri(0)!.AbsolutePath, Does.EndWith("/chat/completions"));
        });
    }

    [Test]
    public async Task ClassifyAsync_Always_RequestsStrictJsonSchemaWithAllowedValues()
    {
        _handler.Enqueue(static () => ChatCompletionResponses.Classification("Billing", "High"));

        await _sut.ClassifyAsync(Subject, Body);

        using var request = _handler.RequestJson(0);
        var format = request.RootElement.GetProperty("response_format");
        var schema = format.GetProperty("json_schema");
        var properties = schema.GetProperty("schema").GetProperty("properties");
        Assert.Multiple(() =>
        {
            Assert.That(format.GetProperty("type").GetString(), Is.EqualTo("json_schema"));
            Assert.That(schema.GetProperty("name").GetString(), Is.EqualTo(TicketClassificationSchema.Name));
            Assert.That(schema.GetProperty("strict").GetBoolean(), Is.True);
            Assert.That(Strings(properties.GetProperty("category").GetProperty("enum")), Is.EqualTo(TriageValueParser.CategoryNames));
            Assert.That(Strings(properties.GetProperty("priority").GetProperty("enum")), Is.EqualTo(TriageValueParser.PriorityNames));
        });
    }

    [Test]
    public async Task ClassifyAsync_Always_SendsModelTemperatureAndTokenLimitFromSettings()
    {
        _handler.Enqueue(static () => ChatCompletionResponses.Classification("Billing", "High"));

        await _sut.ClassifyAsync(Subject, Body);

        using var request = _handler.RequestJson(0);
        var root = request.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("model").GetString(), Is.EqualTo(Model));
            Assert.That(root.GetProperty("temperature").GetDouble(), Is.Zero);
            Assert.That(root.GetProperty("max_completion_tokens").GetInt32(), Is.EqualTo(200));
        });
    }

    [Test]
    public async Task ClassifyAsync_Always_SendsSystemPromptThenEncodedTicket()
    {
        _handler.Enqueue(static () => ChatCompletionResponses.Classification("Technical", "Medium"));

        await _sut.ClassifyAsync("Display issue <script>alert('xss')</script>", "The dashboard is blank.");

        using var request = _handler.RequestJson(0);
        var messages = request.RootElement.GetProperty("messages");
        Assert.Multiple(() =>
        {
            Assert.That(messages.GetArrayLength(), Is.EqualTo(2));
            Assert.That(messages[0].GetProperty("role").GetString(), Is.EqualTo("system"));
            Assert.That(Text(messages[0]), Is.EqualTo(TicketClassificationPrompt.SystemMessage));
            Assert.That(messages[1].GetProperty("role").GetString(), Is.EqualTo("user"));
            Assert.That(Text(messages[1]), Is.EqualTo(TicketClassificationPrompt.BuildUserMessage("Display issue <script>alert('xss')</script>", "The dashboard is blank.")));
        });
    }

    [Test]
    public async Task ClassifyAsync_TransientFailuresThenSuccess_RetriesAndReturnsClassification()
    {
        _handler.Enqueue(static () => ChatCompletionResponses.Error(HttpStatusCode.ServiceUnavailable));
        _handler.Enqueue(static () => ChatCompletionResponses.Error(HttpStatusCode.TooManyRequests));
        _handler.Enqueue(static () => ChatCompletionResponses.Classification("Shipping", "Medium"));

        var result = await _sut.ClassifyAsync("Wrong size delivered", "I ordered size M and received XL. How do I exchange it?");

        Assert.Multiple(() =>
        {
            Assert.That(result.Category, Is.EqualTo(TicketCategory.Shipping));
            Assert.That(_handler.RequestCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void ClassifyAsync_PersistentServerError_ThrowsTriageProviderUnavailableAfterAllRetries()
    {
        _handler.RespondAlways(static () => ChatCompletionResponses.Error(HttpStatusCode.InternalServerError));

        var exception = Assert.ThrowsAsync<TriageProviderUnavailableException>(async () => await _sut.ClassifyAsync(Subject, Body));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("HTTP 500"));
            Assert.That(_handler.RequestCount, Is.EqualTo(1 + MaxRetryAttempts));
        });
    }

    [Test]
    public void ClassifyAsync_NetworkFailure_ThrowsTriageProviderUnavailableAfterAllRetries()
    {
        _handler.RespondAlways(static () => throw new HttpRequestException("Connection refused"));

        Assert.ThrowsAsync<TriageProviderUnavailableException>(async () => await _sut.ClassifyAsync(Subject, Body));

        Assert.That(_handler.RequestCount, Is.EqualTo(1 + MaxRetryAttempts));
    }

    [Test]
    public void ClassifyAsync_BadRequest_ThrowsTriageProviderUnavailableWithoutRetrying()
    {
        _handler.RespondAlways(static () => ChatCompletionResponses.Error(HttpStatusCode.BadRequest));

        Assert.ThrowsAsync<TriageProviderUnavailableException>(async () => await _sut.ClassifyAsync(Subject, Body));

        Assert.That(_handler.RequestCount, Is.EqualTo(1));
    }

    // A bad key, missing permission or unknown model is our setup, not the ticket: it must not count as a ticket failure.
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.NotFound)]
    public void ClassifyAsync_RejectedConfiguration_ThrowsTriageProviderConfigurationWithoutRetrying(HttpStatusCode status)
    {
        _handler.RespondAlways(() => ChatCompletionResponses.Error(status));

        Assert.ThrowsAsync<TriageProviderConfigurationException>(async () => await _sut.ClassifyAsync(Subject, Body));

        Assert.That(_handler.RequestCount, Is.EqualTo(1));
    }

    // The model's answer is never retried at the HTTP level: a bad answer is a failed triage attempt for the worker.
    [Test]
    public void ClassifyAsync_ValueOutsideAllowedSet_ThrowsInvalidTriageResponseWithoutRetrying()
    {
        _handler.RespondAlways(static () => ChatCompletionResponses.Classification("Shipping", "Urgent"));

        Assert.ThrowsAsync<InvalidTriageResponseException>(async () => await _sut.ClassifyAsync("Wrong size delivered", "I ordered size M and received XL."));

        Assert.That(_handler.RequestCount, Is.EqualTo(1));
    }

    [Test]
    public void ClassifyAsync_Refusal_ThrowsInvalidTriageResponseException()
    {
        _handler.Enqueue(static () => ChatCompletionResponses.Completion(content: null, refusal: "I can't help with that."));

        Assert.ThrowsAsync<InvalidTriageResponseException>(async () => await _sut.ClassifyAsync(Subject, Body));
    }

    [Test]
    public void ClassifyAsync_TruncatedResponse_ThrowsInvalidTriageResponseException()
    {
        _handler.Enqueue(static () => ChatCompletionResponses.Completion("""{"category":"Billing","prio""", finishReason: "length"));

        Assert.ThrowsAsync<InvalidTriageResponseException>(async () => await _sut.ClassifyAsync(Subject, Body));
    }

    [Test]
    public void AddInfrastructure_MissingModel_FailsSettingsValidation()
    {
        var settings = Settings();
        settings.Remove("OpenAi:ClassificationModel");
        using var handler = new StubHttpMessageHandler();
        using var provider = BuildProvider(handler, settings);

        Assert.Throws<OptionsValidationException>(() => _ = provider.GetRequiredService<IOptions<OpenAiSettings>>().Value);
    }

    private static Dictionary<string, string?> Settings()
    {
        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["OpenAi:ApiKey"] = "test-key",
            ["OpenAi:Endpoint"] = "https://openai.test/v1",
            ["OpenAi:ClassificationModel"] = Model,
            ["OpenAi:MaxRetryAttempts"] = MaxRetryAttempts.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["OpenAi:RetryBaseDelay"] = "00:00:00.001",
        };
    }

    private static ServiceProvider BuildProvider(StubHttpMessageHandler handler, Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        services.AddHttpClient(OpenAiHttpClient.ClassificationName).ConfigurePrimaryHttpMessageHandler(() => handler);

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static List<string?> Strings(JsonElement array)
    {
        var values = new List<string?>();
        foreach (var item in array.EnumerateArray())
            values.Add(item.GetString());

        return values;
    }

    // The SDK may send message content as a plain string or as an array of text parts.
    private static string Text(JsonElement message)
    {
        var content = message.GetProperty("content");
        if (content.ValueKind == JsonValueKind.String)
            return content.GetString() ?? string.Empty;

        var builder = new StringBuilder();
        foreach (var part in content.EnumerateArray())
            builder.Append(part.GetProperty("text").GetString());

        return builder.ToString();
    }
}
