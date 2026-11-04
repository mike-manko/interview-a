import { TestBed } from '@angular/core/testing';
import { TenantContext } from './tenant-context';

describe('TenantContext', () => {
  beforeEach(() => localStorage.clear());

  it('starts on the first known tenant', () => {
    expect(TestBed.inject(TenantContext).current()).toBe('contoso');
  });

  it('switches tenant and remembers it for the next visit', () => {
    TestBed.inject(TenantContext).select('fabrikam');

    expect(TestBed.inject(TenantContext).current()).toBe('fabrikam');
    expect(localStorage.getItem('support-triage.tenant')).toBe('fabrikam');
  });

  it('restores a remembered tenant', () => {
    localStorage.setItem('support-triage.tenant', 'acme');

    expect(TestBed.inject(TenantContext).current()).toBe('acme');
  });

  it('ignores a remembered value that is not a tenant id', () => {
    localStorage.setItem('support-triage.tenant', 'Not A Tenant');

    expect(TestBed.inject(TenantContext).current()).toBe('contoso');
  });

  it('refuses an invalid tenant id', () => {
    const tenant = TestBed.inject(TenantContext);

    expect(() => tenant.select('Contoso')).toThrow();
    expect(tenant.current()).toBe('contoso');
  });
});
