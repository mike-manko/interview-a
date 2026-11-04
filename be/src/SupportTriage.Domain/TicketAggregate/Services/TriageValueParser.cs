using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using SupportTriage.Domain.TicketAggregate.Enums;

namespace SupportTriage.Domain.TicketAggregate.Services;

// The allowed categories and priorities as text. Every value that arrives as a string (model output, seed data) goes
// through here. Matching is exact and ordinal: Enum.TryParse would also accept "billing", "3", " Billing" and
// "Billing, Technical", and none of those is an allowed value.
public static class TriageValueParser
{
    public static IReadOnlyList<string> CategoryNames { get; } = Array.AsReadOnly(Enum.GetNames<TicketCategory>());

    public static IReadOnlyList<string> PriorityNames { get; } = Array.AsReadOnly(Enum.GetNames<TicketPriority>());

    private static readonly FrozenDictionary<string, TicketCategory> Categories = BuildLookup<TicketCategory>();

    private static readonly FrozenDictionary<string, TicketPriority> Priorities = BuildLookup<TicketPriority>();

    public static bool TryParseCategory([NotNullWhen(true)] string? value, out TicketCategory category)
    {
        category = default;

        return value is not null && Categories.TryGetValue(value, out category);
    }

    public static bool TryParsePriority([NotNullWhen(true)] string? value, out TicketPriority priority)
    {
        priority = default;

        return value is not null && Priorities.TryGetValue(value, out priority);
    }

    private static FrozenDictionary<string, TEnum> BuildLookup<TEnum>()
        where TEnum : struct, Enum
    {
        var values = Enum.GetValues<TEnum>();
        var lookup = new Dictionary<string, TEnum>(values.Length, StringComparer.Ordinal);
        foreach (var value in values)
            lookup.Add(value.ToString(), value);

        return lookup.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
