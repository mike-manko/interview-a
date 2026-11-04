using SupportTriage.Domain.SharedKernel.Exceptions;

namespace SupportTriage.Domain.SharedKernel;

public abstract class TimestampedTenantEntityBase<TId> : TenantEntityBase<TId>, ISoftRemovable
    where TId : notnull
{
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    protected TimestampedTenantEntityBase()
    {
    }

    public void SoftRemove(IDateTime dateTime)
    {
        Guard.EnsureIsNotNull(dateTime, SharedKernelErrorMessages.DateTimeProviderIsRequired);

        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAt = dateTime.UtcNow;
    }

    public void Restore()
    {
        if (!IsDeleted)
            return;

        IsDeleted = false;
        DeletedAt = null;
    }

    protected void StampCreate(IDateTime dateTime)
    {
        Guard.EnsureIsNotNull(dateTime, SharedKernelErrorMessages.DateTimeProviderIsRequired);

        CreatedAt = dateTime.UtcNow;
    }

    protected void StampUpdate(IDateTime dateTime)
    {
        Guard.EnsureIsNotNull(dateTime, SharedKernelErrorMessages.DateTimeProviderIsRequired);

        UpdatedAt = dateTime.UtcNow;
    }

    protected void StampUpdate(DateTime occurredAt) => UpdatedAt = occurredAt;

    protected void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new EntityRemovedException(SharedKernelErrorMessages.EntityIsRemoved.WithMessageFormat(GetType().Name, Id).WithKey(Id));
    }
}
