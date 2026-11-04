using SupportTriage.Domain.SharedKernel.Exceptions;

namespace SupportTriage.Domain.SharedKernel;

public abstract class TenantEntityBase<TId> : EntityBase<TId>, ITenantOwned
    where TId : notnull
{
    public const int MaxTenantIdLength = 64;

    public string TenantId { get; protected set; } = null!;

    protected TenantEntityBase()
    {
    }

    protected void EnsureSameTenant(ITenantOwned other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (!string.Equals(other.TenantId, TenantId, StringComparison.Ordinal))
            throw new CrossTenantException(SharedKernelErrorMessages.CrossTenantLink.WithMessageFormat(TenantId, other.TenantId));
    }
}
