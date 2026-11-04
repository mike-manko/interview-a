using System.Text.Json;

namespace SupportTriage.Infrastructure.Tests.Mocks;

// Sits below the resilience handler, so every retry reaches it and is recorded.
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    public int RequestCount => _requestBodies.Count;

    private readonly Queue<Func<HttpResponseMessage>> _responses = new();

    private readonly List<string> _requestBodies = new();

    private readonly List<Uri?> _requestUris = new();

    private Func<HttpResponseMessage>? _fallback;

    public void Enqueue(Func<HttpResponseMessage> response) => _responses.Enqueue(response);

    public void RespondAlways(Func<HttpResponseMessage> response) => _fallback = response;

    public JsonDocument RequestJson(int index) => JsonDocument.Parse(_requestBodies[index]);

    public Uri? RequestUri(int index) => _requestUris[index];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        _requestBodies.Add(body);
        _requestUris.Add(request.RequestUri);

        var response = _responses.Count > 0 ? _responses.Dequeue() : _fallback ?? throw new InvalidOperationException("No stub response is queued.");

        return response();
    }
}
