// Same rule as the API's TenantIdFormat: 1-64 lowercase letters, digits and dashes.
const tenantIdPattern = /^[a-z0-9-]{1,64}$/;

// The API has no endpoint that lists tenants, so the switcher offers the seed data's tenants and accepts any other id.
export const knownTenants: readonly string[] = ['contoso', 'fabrikam'];

export function isTenantId(value: string): boolean {
  return tenantIdPattern.test(value);
}
