using System.Globalization;
using Newtonsoft.Json.Converters;

namespace SupportTriage.Persistence.Serialization;

// Writes every date in one fixed-width UTC format, so string comparison in queries (nextAttemptAt <= @now) and
// ORDER BY match time order. Newtonsoft's default drops trailing zero fractions, and "...06Z" sorts after "...06.5Z".
internal sealed class UtcDateTimeConverter : IsoDateTimeConverter
{
    public const string Format = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff'Z'";

    public UtcDateTimeConverter()
    {
        DateTimeFormat = Format;
        DateTimeStyles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal;
        Culture = CultureInfo.InvariantCulture;
    }

    public static string ToQueryValue(DateTime value) => value.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture);
}
