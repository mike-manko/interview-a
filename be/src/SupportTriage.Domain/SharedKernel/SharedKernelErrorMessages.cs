namespace SupportTriage.Domain.SharedKernel;

// Codes 100-999 belong to the shared kernel; each aggregate owns its own range (errors.md).
internal static class SharedKernelErrorMessages
{
    public static readonly ErrorMessage IdGeneratorIsRequired = new("An id generator is required.", 100);

    public static readonly ErrorMessage DateTimeProviderIsRequired = new("A date-time provider is required.", 101);

    public static readonly ErrorMessage ValueOutOfRange = new("{0} must be between {1} and {2}; {3} was given.", 102);

    public static readonly ErrorMessage EntityIsRemoved = new("{0} {1} has been removed.", 103, ErrorType.Conflict);

    public static readonly ErrorMessage CrossTenantLink = new("Entities of tenant {0} and tenant {1} cannot be linked.", 104, ErrorType.Forbidden);

    public static readonly ErrorMessage DateTimeMustBeUtc = new("A frozen clock needs a UTC instant; the kind was {0}.", 105);

    public static readonly ErrorMessage TenantIdIsRequired = new("A tenant id is required.", 106);

    public static readonly ErrorMessage TenantIdIsTooLong = new("The tenant id must not be longer than {0} characters.", 107);
}
