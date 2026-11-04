using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Application.Common.Constants;

// Codes 2100-2199 belong to tenancy in the Application layer (errors.md).
internal static class TenantErrorMessages
{
    public static readonly ErrorMessage TenantIdIsInvalid = new("Tenant id '{0}' is not valid: use 1-64 lowercase letters, digits and dashes.", 2101);

    public static readonly ErrorMessage TenantScopeIsNotAssigned = new("No tenant scope is assigned.", 2102, ErrorType.Unexpected);

    public static readonly ErrorMessage TenantScopeIsAlreadyAssigned = new("The tenant scope is already assigned and cannot change.", 2103, ErrorType.Unexpected);

    public static readonly ErrorMessage TenantAccessDenied = new("The current scope cannot access tenant {0}.", 2104, ErrorType.Forbidden);

    public static readonly ErrorMessage SingleTenantIsRequired = new("This operation needs a single tenant scope.", 2105, ErrorType.Unexpected);
}
