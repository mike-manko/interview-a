import { tenantHue } from './tenant-hue';

describe('tenantHue', () => {
  it('gives the same tenant the same hue every time', () => {
    expect(tenantHue('contoso')).toBe(tenantHue('contoso'));
  });

  it('gives the seed tenants clearly different hues', () => {
    expect(Math.abs(tenantHue('contoso') - tenantHue('fabrikam'))).toBeGreaterThan(60);
  });

  it('stays within the colour wheel', () => {
    for (const tenantId of ['a', 'contoso', 'fabrikam', 'tenant-1', 'x'.repeat(64)]) {
      const hue = tenantHue(tenantId);
      expect(hue).toBeGreaterThanOrEqual(0);
      expect(hue).toBeLessThan(360);
      expect(Number.isInteger(hue)).toBe(true);
    }
  });
});
