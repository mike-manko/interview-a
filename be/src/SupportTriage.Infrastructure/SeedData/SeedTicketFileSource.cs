using System.Text.Json;
using Microsoft.Extensions.Options;
using SupportTriage.Application.Tickets.Dto;
using SupportTriage.Application.Tickets.Interfaces;
using SupportTriage.Application.Tickets.Settings;

namespace SupportTriage.Infrastructure.SeedData;

// Reads seed_data.json: an array of tickets with camelCase fields. Category and priority are passed on as text; the
// domain decides whether they are allowed values.
internal sealed class SeedTicketFileSource : ISeedTicketSource
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly SeedDataSettings _settings;

    public SeedTicketFileSource(IOptions<SeedDataSettings> settings)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<IReadOnlyList<SeedTicketDto>> ReadSeedTicketsAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.FilePath))
            return [];

        var path = Path.GetFullPath(_settings.FilePath, AppContext.BaseDirectory);
        await using var stream = File.OpenRead(path);
        var tickets = await JsonSerializer.DeserializeAsync<SeedTicketDto[]>(stream, SerializerOptions, cancellationToken);

        return tickets ?? [];
    }
}
