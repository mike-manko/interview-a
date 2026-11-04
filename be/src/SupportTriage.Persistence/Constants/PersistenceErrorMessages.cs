using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Persistence.Constants;

// Codes 4000-4099 belong to the Persistence layer (errors.md).
internal static class PersistenceErrorMessages
{
    public static readonly ErrorMessage ContinuationTokenIsInvalid = new("The continuation token is not valid; start again from the first page.", 4001);
}
