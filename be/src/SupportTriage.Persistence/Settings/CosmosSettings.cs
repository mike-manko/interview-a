using System.ComponentModel.DataAnnotations;

namespace SupportTriage.Persistence.Settings;

internal sealed class CosmosSettings
{
    public const string SectionName = "Cosmos";

    [Required]
    public Uri Endpoint { get; set; } = null!;

    // Local emulator only. Production must use Microsoft Entra ID with data-plane RBAC instead (cosmos.md).
    [Required]
    public string Key { get; set; } = null!;

    [Required]
    public string DatabaseName { get; set; } = null!;

    [Required]
    public string TicketsContainerName { get; set; } = null!;
}
