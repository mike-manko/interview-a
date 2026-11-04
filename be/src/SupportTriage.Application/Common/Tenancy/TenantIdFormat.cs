using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace SupportTriage.Application.Common.Tenancy;

// Tenant ids are partition keys, which are case-sensitive: "Contoso" and "contoso" would be two tenants. Only
// lowercase letters, digits and inner dashes are accepted, so a typo cannot create or reach another partition.
public static partial class TenantIdFormat
{
    public const int MaxLength = 64;

    private const int MatchTimeoutMilliseconds = 100;

    public static bool IsValid([NotNullWhen(true)] string? tenantId)
    {
        return tenantId is { Length: > 0 and <= MaxLength } && Pattern().IsMatch(tenantId);
    }

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$", RegexOptions.CultureInvariant, MatchTimeoutMilliseconds)]
    private static partial Regex Pattern();
}
