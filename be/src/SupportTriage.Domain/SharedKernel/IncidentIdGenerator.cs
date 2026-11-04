using System.Globalization;

namespace SupportTriage.Domain.SharedKernel;

// Incident ids correlate one failure between the log and the client response; they are not business identifiers.
internal static class IncidentIdGenerator
{
    public static string Next() => Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
}
