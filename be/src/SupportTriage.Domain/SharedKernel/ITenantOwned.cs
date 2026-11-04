namespace SupportTriage.Domain.SharedKernel;

public interface ITenantOwned
{
    string TenantId { get; }
}
