using System.Globalization;
using System.Text.RegularExpressions;

namespace SupportTriage.Domain.TicketAggregate.Services;

// PCI DSS forbids storing a card security code, and full card numbers must not be stored or sent to third parties
// (the LLM provider). Ticket text is masked before it is stored: a 13-19 digit sequence that passes the Luhn check
// becomes "[card ending 1234]", and a labelled security code becomes "[removed]".
public static partial class PaymentCardMasker
{
    public const string SecurityCodeMask = "[removed]";

    private const string CardNumberMaskFormat = "[card ending {0}]";

    private const int MinCardDigits = 13;

    private const int MaxCardDigits = 19;

    private const int VisibleDigits = 4;

    private const int MatchTimeoutMilliseconds = 1_000;

    public static string Mask(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.AsSpan().IndexOfAnyInRange('0', '9') < 0)
            return text;

        var masked = CardNumberCandidate().Replace(text, static match => MaskCardNumber(match.Value));

        return SecurityCode().Replace(masked, static match => match.Groups["label"].Value + " " + SecurityCodeMask);
    }

    private static string MaskCardNumber(string candidate)
    {
        Span<int> digits = stackalloc int[MaxCardDigits];
        var count = 0;
        foreach (var character in candidate)
        {
            if (char.IsAsciiDigit(character))
            {
                if (count == MaxCardDigits)
                    return candidate;

                digits[count++] = character - '0';
            }
        }

        if (count < MinCardDigits || !PassesLuhnCheck(digits[..count]))
            return candidate;

        Span<char> lastDigits = stackalloc char[VisibleDigits];
        for (var i = 0; i < VisibleDigits; i++)
            lastDigits[i] = (char)('0' + digits[count - VisibleDigits + i]);

        return string.Format(CultureInfo.InvariantCulture, CardNumberMaskFormat, lastDigits.ToString());
    }

    private static bool PassesLuhnCheck(ReadOnlySpan<int> digits)
    {
        var sum = 0;
        var doubleIt = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var digit = digits[i];
            if (doubleIt)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }

            sum += digit;
            doubleIt = !doubleIt;
        }

        return sum % 10 == 0;
    }

    // Digits optionally separated by single spaces or dashes, not touching other digits.
    [GeneratedRegex(@"(?<!\d)\d(?:[ -]?\d){12,18}(?!\d)", RegexOptions.CultureInvariant, MatchTimeoutMilliseconds)]
    private static partial Regex CardNumberCandidate();

    [GeneratedRegex(@"\b(?<label>CVV2?|CVC2?|CID|security code)\b\s*[:=]?\s*\d{3,4}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, MatchTimeoutMilliseconds)]
    private static partial Regex SecurityCode();
}
