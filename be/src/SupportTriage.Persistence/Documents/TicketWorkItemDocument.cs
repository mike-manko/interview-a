namespace SupportTriage.Persistence.Documents;

internal sealed class TicketWorkItemDocument
{
    public string Id { get; set; } = null!;

    public string TenantId { get; set; } = null!;
}
