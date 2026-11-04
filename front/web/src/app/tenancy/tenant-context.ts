import { Service, signal } from '@angular/core';
import { isTenantId, knownTenants } from './tenant-id';

const storageKey = 'support-triage.tenant';

// Stand-in for real authentication: the API trusts the X-Tenant-Id header, so the app sends whichever tenant is
// selected here. Every ticket request reads current(), so switching tenants re-requests everything for the new one.
@Service()
export class TenantContext {
  private readonly selected = signal(readStoredTenant() ?? knownTenants[0]);
  readonly current = this.selected.asReadonly();

  select(tenantId: string): void {
    if (!isTenantId(tenantId)) {
      throw new Error(`"${tenantId}" is not a valid tenant id.`);
    }
    this.selected.set(tenantId);
    storeTenant(tenantId);
  }
}

// Storage can be unavailable (private mode, blocked site data); the app then starts on the first known tenant.
function readStoredTenant(): string | undefined {
  try {
    const stored = globalThis.localStorage?.getItem(storageKey) ?? undefined;
    return stored !== undefined && isTenantId(stored) ? stored : undefined;
  } catch {
    return undefined;
  }
}

function storeTenant(tenantId: string): void {
  try {
    globalThis.localStorage?.setItem(storageKey, tenantId);
  } catch {
    // Remembering the tenant is a convenience; the selection still works for this session.
  }
}
