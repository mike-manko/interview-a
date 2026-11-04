using SupportTriage.Domain.SharedKernel;
using SupportTriage.Domain.TicketAggregate.Constants;
using SupportTriage.Domain.TicketAggregate.Enums;
using SupportTriage.Domain.TicketAggregate.Exceptions;

namespace SupportTriage.Domain.TicketAggregate.ValueObjects;

// The result of the screening step that runs before classification. Source says what decided it (a rule or the
// screening model and prompt version); Detail names the matched rule, if any.
public sealed record TicketScreening
{
    public const int MaxSourceLength = 200;

    public const int MaxDetailLength = 200;

    public const string EmptyTicketRuleSource = "rule:empty-ticket";

    public const string RuleSourcePrefix = "rule:prompt-injection:";

    public ScreeningVerdict Verdict { get; }

    public string Source { get; }

    public string? Detail { get; }

    // A ticket without subject and body cannot carry instructions.
    public static readonly TicketScreening ForEmptyTicket = new(ScreeningVerdict.Safe, EmptyTicketRuleSource, null);

    private TicketScreening()
    {
        Source = string.Empty;
    }

    public TicketScreening(ScreeningVerdict verdict, string source, string? detail)
    {
        List<ErrorMessage>? errors = null;
        if (!Enum.IsDefined(verdict))
            (errors ??= []).Add(TicketErrorMessages.ScreeningVerdictIsNotDefined.WithMessageFormat((int)verdict).WithKey(nameof(verdict)));

        if (string.IsNullOrWhiteSpace(source))
            (errors ??= []).Add(TicketErrorMessages.ScreeningSourceIsRequired.WithKey(nameof(source)));
        else if (source.Length > MaxSourceLength)
            (errors ??= []).Add(TicketErrorMessages.ScreeningSourceIsTooLong.WithMessageFormat(MaxSourceLength).WithKey(nameof(source)));

        if (errors is not null)
            throw new InvalidTicketScreeningException(errors);

        Verdict = verdict;
        Source = source;
        Detail = detail is null || detail.Length <= MaxDetailLength ? detail : detail[..MaxDetailLength];
    }

    public static TicketScreening FlaggedByRule(string ruleName)
    {
        Guard.EnsureIsNotNullOrWhiteSpace(ruleName, TicketErrorMessages.ScreeningSourceIsRequired);

        return new TicketScreening(ScreeningVerdict.PromptInjection, RuleSourcePrefix + ruleName, ruleName);
    }
}
