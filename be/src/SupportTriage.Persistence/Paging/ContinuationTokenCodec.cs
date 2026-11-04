using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SupportTriage.Persistence.Paging;

// Cosmos continuation tokens are JSON; clients get them base64url-encoded so they fit in a query string unchanged.
internal static class ContinuationTokenCodec
{
    public static string? Encode(string? token) => token is null ? null : Base64Url.EncodeToString(Encoding.UTF8.GetBytes(token));

    public static bool TryDecode(string? encoded, [NotNullWhen(true)] out string? token)
    {
        token = null;
        if (string.IsNullOrEmpty(encoded) || !Base64Url.IsValid(encoded))
            return false;

        token = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(encoded));

        return true;
    }
}
