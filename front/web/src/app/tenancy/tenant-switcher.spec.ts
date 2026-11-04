import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { expectAccessible } from '../../testing/expect-accessible';
import { TenantContext } from './tenant-context';
import { tenantHue } from './tenant-hue';
import { TenantSwitcher } from './tenant-switcher';

@Component({ template: '' })
class Blank {}

describe('TenantSwitcher', () => {
  let fixture: ComponentFixture<TenantSwitcher>;
  let element: HTMLElement;
  let hidePopover: ReturnType<typeof vi.fn<() => void>>;

  function query<T extends Element>(selector: string): T {
    const found = element.querySelector<T>(selector);
    if (!found) {
      throw new Error(`${selector} missing`);
    }
    return found;
  }

  function trigger(): HTMLButtonElement {
    return query<HTMLButtonElement>('button[popovertarget]');
  }

  function knownTenantButton(tenantId: string): HTMLButtonElement {
    const buttons = [...element.querySelectorAll<HTMLButtonElement>('[popover] li button')];
    const found = buttons.find((button) => button.textContent?.trim().endsWith(tenantId));
    if (!found) {
      throw new Error(`no button for ${tenantId}`);
    }
    return found;
  }

  function announcement(): string {
    return query('p[aria-live="polite"]').textContent?.trim() ?? '';
  }

  async function enterOtherTenant(value: string): Promise<void> {
    const input = query<HTMLInputElement>('#other-tenant');
    input.value = value;
    input.dispatchEvent(new Event('input'));
    query<HTMLButtonElement>('button[type="submit"]').click();
    await fixture.whenStable();
  }

  beforeEach(async () => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([{ path: 'tickets', component: Blank }])],
    });
    await TestBed.inject(Router).navigateByUrl('/tickets?priority=High');
    fixture = TestBed.createComponent(TenantSwitcher);
    element = fixture.nativeElement;
    await fixture.whenStable();
    // jsdom has no Popover API; opening and closing are the browser's job, the component only asks it to close.
    hidePopover = vi.fn<() => void>();
    query<HTMLElement>('[popover]').hidePopover = hidePopover;
  });

  afterEach(() => localStorage.clear());

  it('shows the current tenant on a labelled button with its monogram and hue', async () => {
    expect(trigger().textContent?.replace(/\s+/g, ' ').trim()).toBe('Tenant C contoso');
    expect(trigger().style.getPropertyValue('--tenant-hue')).toBe(String(tenantHue('contoso')));
    expect(query('span[aria-hidden="true"]').textContent?.trim()).toBe('C');
    await expectAccessible(element);
  });

  it('opens a native popover panel from the button', () => {
    const panelId = trigger().getAttribute('popovertarget');

    expect(panelId).toBeTruthy();
    expect(element.querySelector(`#${panelId}`)?.hasAttribute('popover')).toBe(true);
  });

  it('marks the current tenant among the known tenants', () => {
    expect(knownTenantButton('contoso').getAttribute('aria-current')).toBe('true');
    expect(knownTenantButton('fabrikam').getAttribute('aria-current')).toBe('false');
  });

  it('switches to a known tenant, returns to the queue without filters and announces it', async () => {
    knownTenantButton('fabrikam').click();
    await fixture.whenStable();

    expect(TestBed.inject(TenantContext).current()).toBe('fabrikam');
    expect(TestBed.inject(Router).url).toBe('/tickets');
    expect(hidePopover).toHaveBeenCalled();
    expect(announcement()).toBe('Now showing tickets for fabrikam');
    expect(trigger().textContent).toContain('fabrikam');
    await expectAccessible(element);
  });

  it('does nothing when the current tenant is picked again', async () => {
    knownTenantButton('contoso').click();
    await fixture.whenStable();

    expect(TestBed.inject(TenantContext).current()).toBe('contoso');
    expect(TestBed.inject(Router).url).toBe('/tickets?priority=High');
    expect(announcement()).toBe('');
  });

  it('rejects an other tenant id that the API would refuse', async () => {
    await enterOtherTenant('Not valid!');

    expect(query('#other-tenant-error').textContent?.trim()).toBe(
      'Use 1-64 lowercase letters, digits or dashes.',
    );
    expect(query('#other-tenant').getAttribute('aria-invalid')).toBe('true');
    expect(TestBed.inject(TenantContext).current()).toBe('contoso');
    expect(TestBed.inject(Router).url).toBe('/tickets?priority=High');
    await expectAccessible(element);
  });

  it('switches to any other valid tenant id and clears the field', async () => {
    await enterOtherTenant('acme-1');

    expect(TestBed.inject(TenantContext).current()).toBe('acme-1');
    expect(TestBed.inject(Router).url).toBe('/tickets');
    expect(announcement()).toBe('Now showing tickets for acme-1');
    expect(query<HTMLInputElement>('#other-tenant').value).toBe('');
    expect(query('#other-tenant-error').textContent?.trim()).toBe('');
  });
});
