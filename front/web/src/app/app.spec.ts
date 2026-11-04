import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { expectAccessible } from '../testing/expect-accessible';
import { App } from './app';
import { TenantContext } from './tenancy/tenant-context';
import { tenantHue } from './tenancy/tenant-hue';

describe('App', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
  });

  afterEach(() => localStorage.clear());

  it('links to the ticket list and to the create form', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const links = [...fixture.nativeElement.querySelectorAll('header a')].map(
      (link: HTMLAnchorElement) => [link.textContent?.trim(), link.getAttribute('href')],
    );
    expect(links).toEqual([
      ['Support triage', '/tickets'],
      ['New ticket', '/tickets/new'],
    ]);
    await expectAccessible(fixture.nativeElement);
  });

  it('shows the tenant switcher in the header', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const switcher = fixture.nativeElement.querySelector('header app-tenant-switcher button');
    expect(switcher?.textContent?.replace(/\s+/g, ' ').trim()).toBe('Tenant C contoso');
  });

  it('colours the top strip with the current tenant hue', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const strip: HTMLElement = fixture.nativeElement.querySelector('div[aria-hidden="true"]');

    expect(strip.style.getPropertyValue('--tenant-hue')).toBe(String(tenantHue('contoso')));

    TestBed.inject(TenantContext).select('fabrikam');
    await fixture.whenStable();

    expect(strip.style.getPropertyValue('--tenant-hue')).toBe(String(tenantHue('fabrikam')));
  });

  it('lets keyboard users skip to the main content', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const skipLink: HTMLAnchorElement = fixture.nativeElement.querySelector('a');
    expect(skipLink.textContent?.trim()).toBe('Skip to content');
    expect(fixture.nativeElement.querySelector(skipLink.hash)?.tagName).toBe('MAIN');
  });
});
