namespace SupportTriage.Domain.SharedKernel;

public interface ISoftRemovable
{
    bool IsDeleted { get; }

    DateTime? DeletedAt { get; }
}
