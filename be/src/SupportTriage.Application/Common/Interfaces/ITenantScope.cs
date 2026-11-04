namespace SupportTriage.Application.Common.Interfaces;

// The tenant the current scope works for. Assigned once at every entry point: the HTTP middleware (X-Tenant-Id), the
// triage worker (per ticket) and the seed import (unrestricted). Repositories and query sources read it; individual
// queries never add their own tenant filter.
public interface ITenantScope
{
    bool IsScopeAssigned { get; }

    bool HasUnrestrictedAccess { get; }

    void AssignScope(string tenantId);

    void AssignUnrestrictedScope();

    // The single tenant of this scope; throws when none is assigned or the scope is unrestricted.
    string RequireTenantId();

    void EnsureCanAccessTenant(string tenantId);
}
