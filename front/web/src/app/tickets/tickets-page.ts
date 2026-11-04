import { DatePipe } from '@angular/common';
import { Component, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { describeApiError } from '../api/describe-api-error';
import { TenantContext } from '../tenancy/tenant-context';
import { groupTickets } from './group-tickets';
import { matchesSearch } from './matches-search';
import { ticketCountLabel } from './ticket-count-label';
import { autoRefreshMilliseconds, ticketFeed } from './ticket-feed';
import { TicketFilters } from './ticket-filters';
import {
  QueryParamValue,
  sameTicketQuery,
  TicketListView,
  toTicketListParams,
  toTicketListView,
  TriageFilter,
  withoutFilters,
} from './ticket-list-view';
import { filtersClassification } from './ticket-query';
import { TicketQueueTable } from './ticket-queue-table';
import { countTriageBuckets, toTriageBucket } from './triage-bucket';
import { TriageFacets } from './triage-facets';
import { toTriageState } from './triage-state';

@Component({
  selector: 'app-tickets-page',
  imports: [DatePipe, RouterLink, RouterOutlet, TicketFilters, TicketQueueTable, TriageFacets],
  templateUrl: './tickets-page.html',
})
export class TicketsPage {
  private readonly router = inject(Router);
  protected readonly tenant = inject(TenantContext).current;

  // Query params bound by withComponentInputBinding(); the URL is the only place the queue's view is kept.
  readonly category = input<QueryParamValue>();
  readonly priority = input<QueryParamValue>();
  readonly hidden = input<QueryParamValue>();
  readonly group = input<QueryParamValue>();
  readonly state = input<QueryParamValue>();
  readonly q = input<QueryParamValue>();

  protected readonly view = computed(() =>
    toTicketListView({
      category: this.category(),
      priority: this.priority(),
      hidden: this.hidden(),
      group: this.group(),
      state: this.state(),
      q: this.q(),
    }),
  );
  // Compared by value: typing a search or changing the grouping must not re-request the tickets.
  private readonly query = computed(() => this.view().query, { equal: sameTicketQuery });
  protected readonly feed = ticketFeed(this.query);

  private readonly searched = computed(() => {
    const search = this.view().search;
    return this.feed.tickets().filter((ticket) => matchesSearch(ticket, search));
  });
  protected readonly triageCounts = computed(() => countTriageBuckets(this.searched()));
  private readonly shown = computed(() => {
    const triage = this.view().triage;
    if (triage === 'all') {
      return this.searched();
    }
    return this.searched().filter((ticket) => toTriageBucket(ticket) === triage);
  });
  protected readonly groups = computed(() => groupTickets(this.shown(), this.view().grouping));

  // Only an unfiltered empty answer means the tenant has no tickets; with a classification filter the API also
  // leaves out every ticket still in triage.
  protected readonly queueEmpty = computed(
    () =>
      this.feed.tickets().length === 0 &&
      !this.feed.canLoadMore() &&
      !filtersClassification(this.view().query),
  );
  protected readonly shownLabel = computed(() => {
    const shown = this.shown().length;
    const loaded = this.feed.tickets().length;
    if (shown === loaded) {
      return `Showing ${ticketCountLabel(loaded)}`;
    }
    return `Showing ${shown} of ${ticketCountLabel(loaded)} loaded`;
  });
  protected readonly announcement = computed(() => {
    if (this.feed.status() !== 'ready' || this.queueEmpty()) {
      return '';
    }
    return this.shown().length === 0 ? 'No tickets match these filters.' : this.shownLabel();
  });
  protected readonly errorMessage = computed(() => describeApiError(this.feed.error()));
  // The worker retries failed attempts forever (for example while the AI provider rejects the API key), so the
  // queue says so once instead of leaving a column of "retrying" badges unexplained.
  protected readonly retryNotice = computed(() => {
    const retrying = this.feed.tickets().filter((ticket) => toTriageState(ticket) === 'retrying');
    if (retrying.length === 0) {
      return undefined;
    }
    return {
      countLabel: ticketCountLabel(retrying.length),
      latestReason: retrying[0]?.failureMessage ?? null,
    };
  });
  protected readonly refreshIconClass = computed(() =>
    this.feed.refreshing() ? 'size-4 shrink-0 motion-safe:animate-spin' : 'size-4 shrink-0',
  );
  protected readonly autoRefreshSeconds = autoRefreshMilliseconds / 1000;
  protected readonly skeletonRows = [1, 2, 3, 4, 5, 6];
  // On small screens the detail replaces the queue instead of stacking under a long table.
  protected readonly detailOpen = signal(false);

  protected showView(view: TicketListView): void {
    void this.router.navigate([], {
      queryParams: toTicketListParams(view),
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  protected filterByTriage(triage: TriageFilter): void {
    this.showView({ ...this.view(), triage });
  }

  protected showTicketsNeedingAttention(): void {
    this.filterByTriage('attention');
  }

  protected clearFilters(): void {
    this.showView(withoutFilters(this.view()));
  }

  protected refresh(): void {
    this.feed.refresh();
  }

  protected loadMore(): void {
    this.feed.loadMore();
  }

  protected openDetail(): void {
    this.detailOpen.set(true);
  }

  protected closeDetail(): void {
    this.detailOpen.set(false);
  }
}
