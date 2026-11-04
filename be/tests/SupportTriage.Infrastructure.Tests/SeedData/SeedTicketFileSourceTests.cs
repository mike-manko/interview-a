using Microsoft.Extensions.Options;
using SupportTriage.Application.Tickets.Settings;
using SupportTriage.Infrastructure.SeedData;

namespace SupportTriage.Infrastructure.Tests.SeedData;

// Reads the repository's seed_data.json, copied next to the test binaries.
[TestFixture]
public sealed class SeedTicketFileSourceTests
{
    [Test]
    public async Task ReadSeedTicketsAsync_RepositorySeedFile_ReadsEveryTicket()
    {
        var sut = Create("seed_data.json");

        var tickets = await sut.ReadSeedTicketsAsync();

        Assert.Multiple(() =>
        {
            Assert.That(tickets, Has.Count.EqualTo(20));
            Assert.That(tickets[0].Id, Is.EqualTo("t-001"));
            Assert.That(tickets[0].TenantId, Is.EqualTo("contoso"));
            Assert.That(tickets[0].CreatedAt, Is.EqualTo(new DateTime(2026, 10, 28, 8, 12, 0, DateTimeKind.Utc)));
            Assert.That(tickets[0].CreatedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
        });
    }

    // The source passes stored values through as text; the domain decides what is allowed.
    [Test]
    public async Task ReadSeedTicketsAsync_RepositorySeedFile_KeepsValuesOutsideTheAllowedSet()
    {
        var tickets = await Create("seed_data.json").ReadSeedTicketsAsync();

        Assert.Multiple(() =>
        {
            Assert.That(tickets.Single(static t => t.Id == "t-012").Priority, Is.EqualTo("Urgent"));
            Assert.That(tickets.Single(static t => t.Id == "t-013").Category, Is.EqualTo("tech"));
            Assert.That(tickets.Single(static t => t.Id == "t-007").Subject, Is.Empty);
        });
    }

    [Test]
    public async Task ReadSeedTicketsAsync_NoFilePath_ReturnsNothing()
    {
        var tickets = await Create(null).ReadSeedTicketsAsync();

        Assert.That(tickets, Is.Empty);
    }

    private static SeedTicketFileSource Create(string? filePath)
    {
        return new SeedTicketFileSource(Options.Create(new SeedDataSettings { ImportOnStartup = true, FilePath = filePath }));
    }
}
