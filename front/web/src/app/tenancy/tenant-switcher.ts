import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { form, FormField, FormRoot, validate } from '@angular/forms/signals';
import { Router } from '@angular/router';
import { FieldError } from '../shared/field-error/field-error';
import { TenantContext } from './tenant-context';
import { tenantHue } from './tenant-hue';
import { isTenantId, knownTenants } from './tenant-id';

interface TenantOption {
  readonly id: string;
  readonly monogram: string;
  readonly hue: number;
}

interface KnownTenantOption extends TenantOption {
  readonly isCurrent: boolean;
}

interface OtherTenant {
  readonly tenantId: string;
}

const emptyOtherTenant: OtherTenant = { tenantId: '' };

@Component({
  selector: 'app-tenant-switcher',
  imports: [FieldError, FormField, FormRoot],
  templateUrl: './tenant-switcher.html',
  // The panel lives in the top layer (Popover API), so CSS anchor positioning places it under its button. Browsers
  // without anchor positioning keep the popover's default placement, centred in the viewport.
  styles: `
    .trigger {
      anchor-name: --tenant-switcher;
    }

    @supports (position-area: block-end) {
      .panel {
        position-anchor: --tenant-switcher;
        position-area: block-end span-inline-end;
        position-try-fallbacks: flip-inline;
        inset: auto;
        margin: 0.25rem 0 0;
      }
    }
  `,
})
export class TenantSwitcher {
  private readonly tenant = inject(TenantContext);
  private readonly router = inject(Router);

  private readonly panel = viewChild.required<ElementRef<HTMLElement>>('panel');

  protected readonly current = computed(() => toTenantOption(this.tenant.current()));
  protected readonly options = computed(() =>
    knownTenants.map((id): KnownTenantOption => ({
      ...toTenantOption(id),
      isCurrent: id === this.current().id,
    })),
  );
  protected readonly announcement = signal('');
  protected readonly otherForm = form(
    signal<OtherTenant>(emptyOtherTenant),
    (path) => {
      validate(path.tenantId, ({ value }) =>
        isTenantId(value())
          ? undefined
          : { kind: 'tenantId', message: 'Use 1-64 lowercase letters, digits or dashes.' },
      );
    },
    { submission: { action: (field) => this.switchTo(field().value().tenantId) } },
  );

  protected readonly showsOtherError = computed(() => {
    const field = this.otherForm.tenantId();
    return field.touched() && field.invalid();
  });

  protected async switchTo(tenantId: string): Promise<undefined> {
    this.panel().nativeElement.hidePopover();
    if (tenantId === this.tenant.current()) {
      return undefined;
    }
    this.tenant.select(tenantId);
    // The open ticket and the filters belong to the old tenant, so the queue starts over.
    await this.router.navigate(['/tickets']);
    this.otherForm().reset(emptyOtherTenant);
    this.announcement.set(`Now showing tickets for ${tenantId}`);
    return undefined;
  }
}

function toTenantOption(id: string): TenantOption {
  return { id, monogram: id.charAt(0).toUpperCase(), hue: tenantHue(id) };
}
