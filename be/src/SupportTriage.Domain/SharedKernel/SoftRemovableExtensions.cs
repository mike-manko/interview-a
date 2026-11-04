namespace SupportTriage.Domain.SharedKernel;

public static class SoftRemovableExtensions
{
    // Returns the original collection when nothing is removed, so the common case allocates nothing (style.md "Performance").
    public static IReadOnlyCollection<T> WithoutSoftlyDeleted<T>(this IReadOnlyCollection<T> items)
        where T : ISoftRemovable
    {
        ArgumentNullException.ThrowIfNull(items);

        var hasRemoved = false;
        foreach (var item in items)
        {
            if (item.IsDeleted)
            {
                hasRemoved = true;
                break;
            }
        }

        if (!hasRemoved)
            return items;

        var remaining = new List<T>(items.Count);
        foreach (var item in items)
        {
            if (!item.IsDeleted)
                remaining.Add(item);
        }

        return remaining;
    }
}
