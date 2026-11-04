import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { environment } from '../environments/environment';
import { TenantContext } from './tenancy/tenant-context';
import { tenantHue } from './tenancy/tenant-hue';
import { TenantSwitcher } from './tenancy/tenant-switcher';

// The 3px strip is a glanceable "which tenant am I in" cue; its colour comes from the tenant's hue, not a token.
// 90rem (1440px) is the widest the queue and detail layout are designed for.
@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet, TenantSwitcher],
  template: `
    <div
      aria-hidden="true"
      class="h-[3px] w-full bg-[oklch(62%_0.14_var(--tenant-hue))]"
      [style.--tenant-hue]="tenantHue()"
    ></div>
    <a
      href="#main"
      class="sr-only rounded-control bg-surface px-3 py-2 text-primary focus:not-sr-only focus:absolute focus:top-2 focus:left-2"
    >
      Skip to content
    </a>
    <header class="border-b border-border bg-surface">
      <div
        class="mx-auto flex max-w-[90rem] flex-wrap items-center gap-x-4 gap-y-2 px-4 py-3 sm:px-6"
      >
        <a routerLink="/tickets" class="text-base font-semibold text-text">Support triage</a>
        <app-tenant-switcher />
        @if (mockBackend) {
          <p
            class="rounded-control bg-warning-soft px-2 py-1 text-xs font-medium text-warning-text"
          >
            Mock API · data resets on reload
          </p>
        }
        <a
          routerLink="/tickets/new"
          class="ms-auto inline-flex h-9 items-center rounded-control bg-primary px-3 text-sm font-medium text-primary-contrast hover:bg-primary/90 active:bg-primary/80"
        >
          New ticket
        </a>
      </div>
    </header>
    <main id="main" class="mx-auto max-w-[90rem] px-4 py-6 sm:px-6">
      <router-outlet />
    </main>
  `,
})
export class App {
  private readonly tenant = inject(TenantContext);

  protected readonly mockBackend = environment.mockBackend;
  protected readonly tenantHue = computed(() => tenantHue(this.tenant.current()));
}
