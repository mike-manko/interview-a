import { isTenantId, knownTenants } from './tenant-id';

describe('isTenantId', () => {
  it('accepts lowercase letters, digits and dashes', () => {
    expect(isTenantId('contoso')).toBe(true);
    expect(isTenantId('tenant-42')).toBe(true);
  });

  it('rejects empty, uppercase, spaced and over-long ids', () => {
    expect(isTenantId('')).toBe(false);
    expect(isTenantId('Contoso')).toBe(false);
    expect(isTenantId('con toso')).toBe(false);
    expect(isTenantId('a'.repeat(65))).toBe(false);
  });

  it('accepts every known tenant', () => {
    expect(knownTenants.every(isTenantId)).toBe(true);
  });
});
