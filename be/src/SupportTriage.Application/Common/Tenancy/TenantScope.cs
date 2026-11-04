using SupportTriage.Application.Common.Constants;
using SupportTriage.Application.Common.Exceptions;
using SupportTriage.Application.Common.Interfaces;

namespace SupportTriage.Application.Common.Tenancy;

// Scoped: one instance per request, per processed ticket and per import. Once assigned, the tenant cannot change, so a
// scope can never drift from one tenant's partition into another's.
internal sealed class TenantScope : ITenantScope
{
    public bool IsScopeAssigned => _tenantId is not null || HasUnrestrictedAccess;

    public bool HasUnrestrictedAccess { get; private set; }

    private string? _tenantId;

    public void AssignScope(string tenantId)
    {
        if (!TenantIdFormat.IsValid(tenantId))
            throw new TenantScopeException(TenantErrorMessages.TenantIdIsInvalid.WithMessageFormat(tenantId).WithKey(nameof(tenantId)));

        if (string.Equals(_tenantId, tenantId, StringComparison.Ordinal))
            return;

        if (IsScopeAssigned)
            throw new TenantScopeException(TenantErrorMessages.TenantScopeIsAlreadyAssigned);

        _tenantId = tenantId;
    }

    public void AssignUnrestrictedScope()
    {
        if (HasUnrestrictedAccess)
            return;

        if (IsScopeAssigned)
            throw new TenantScopeException(TenantErrorMessages.TenantScopeIsAlreadyAssigned);

        HasUnrestrictedAccess = true;
    }

    public string RequireTenantId()
    {
        if (_tenantId is not null)
            return _tenantId;

        throw new TenantScopeException(HasUnrestrictedAccess ? TenantErrorMessages.SingleTenantIsRequired : TenantErrorMessages.TenantScopeIsNotAssigned);
    }

    public void EnsureCanAccessTenant(string tenantId)
    {
        if (HasUnrestrictedAccess)
            return;

        if (!string.Equals(RequireTenantId(), tenantId, StringComparison.Ordinal))
            throw new TenantScopeException(TenantErrorMessages.TenantAccessDenied.WithMessageFormat(tenantId).WithKey(nameof(tenantId)));
    }
}
