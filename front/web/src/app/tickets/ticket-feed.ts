import { computed, DestroyRef, inject, linkedSignal, Signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval, Subscription } from 'rxjs';
import { describeApiError } from '../api/describe-api-error';
import { isInTriage, Ticket, TicketPage } from './ticket';
import { TicketQuery } from './ticket-query';
import { TicketsApi } from './tickets-api';

// Often enough to watch a new ticket get classified, rarely enough to stay quiet on a queue left open all shift.
export const autoRefreshMilliseconds = 15_000;

export type TicketFeedStatus = 'loading' | 'error' | 'ready';

export interface TicketFeed {
  /** The first page plus every page loaded with "Load more", newest first, each ticket once. */
  readonly tickets: Signal<readonly Ticket[]>;
  /** State of the first page; a reload keeps the tickets on screen and stays 'ready'. */
  readonly status: Signal<TicketFeedStatus>;
  readonly error: Signal<unknown>;
  readonly refreshing: Signal<boolean>;
  readonly updatedAt: Signal<Date | undefined>;
  /** True while tickets are being classified and only the first page is loaded, so reloading it loses nothing. */
  readonly autoRefreshing: Signal<boolean>;
  readonly canLoadMore: Signal<boolean>;
  readonly loadingMore: Signal<boolean>;
  readonly loadMoreError: Signal<string | null>;
  refresh(): void;
  loadMore(): void;
}

interface MorePages {
  readonly tickets: readonly Ticket[];
  readonly continuationToken: string | null;
  readonly loading: boolean;
  readonly error: string | null;
}

/**
 * The queue's tickets for a query: the first page, re-requested when the query or tenant changes, and the pages
 * appended by loadMore(). Call in the page's injection context, so every visit to the queue starts with fresh data
 * and the timer stops when the page is left.
 */
export function ticketFeed(query: () => TicketQuery): TicketFeed {
  const api = inject(TicketsApi);
  const destroyRef = inject(DestroyRef);

  const firstPageResource = api.firstPage(query);
  // value() throws while the resource is in error, so read it only when there is one.
  const firstPage = computed(() =>
    firstPageResource.hasValue() ? firstPageResource.value() : undefined,
  );
  // A new first page (new query, new tenant or a refresh) starts the list over: the old continuation token belongs
  // to a list that no longer exists.
  const morePages = linkedSignal<TicketPage | undefined, MorePages>({
    source: firstPage,
    computation: (page) => ({
      tickets: [],
      continuationToken: page?.continuationToken ?? null,
      loading: false,
      error: null,
    }),
  });

  const tickets = computed(() =>
    withoutRepeats([...(firstPage()?.items ?? []), ...morePages().tickets]),
  );
  const status = computed<TicketFeedStatus>(() => {
    if (firstPage() !== undefined) {
      return 'ready';
    }
    return firstPageResource.status() === 'error' ? 'error' : 'loading';
  });
  // Evaluated when the template reads it, which is in the render right after the page arrives.
  const updatedAt = computed(() => (firstPage() === undefined ? undefined : new Date()));
  const autoRefreshing = computed(
    () => morePages().tickets.length === 0 && !morePages().loading && tickets().some(isInTriage),
  );

  interval(autoRefreshMilliseconds)
    .pipe(takeUntilDestroyed(destroyRef))
    .subscribe(() => {
      if (autoRefreshing() && !firstPageResource.isLoading()) {
        firstPageResource.reload();
      }
    });

  let nextPageRequest: Subscription | undefined;

  function loadMore(): void {
    const basis = firstPage();
    const { continuationToken, loading } = morePages();
    if (basis === undefined || continuationToken === null || loading) {
      return;
    }
    morePages.update((pages) => ({ ...pages, loading: true, error: null }));
    nextPageRequest?.unsubscribe();
    nextPageRequest = api
      .nextPage(query(), continuationToken)
      .pipe(takeUntilDestroyed(destroyRef))
      .subscribe({
        next: (page) => {
          // The list started over while the page was on its way; it belongs to the old one.
          if (firstPage() !== basis) {
            return;
          }
          morePages.update((pages) => ({
            tickets: [...pages.tickets, ...page.items],
            continuationToken: page.continuationToken,
            loading: false,
            error: null,
          }));
        },
        error: (error: unknown) => {
          if (firstPage() !== basis) {
            return;
          }
          morePages.update((pages) => ({
            ...pages,
            loading: false,
            error: describeApiError(error),
          }));
        },
      });
  }

  return {
    tickets,
    status,
    error: computed(() => firstPageResource.error()),
    refreshing: computed(() => firstPageResource.status() === 'reloading'),
    updatedAt,
    autoRefreshing,
    canLoadMore: computed(() => morePages().continuationToken !== null),
    loadingMore: computed(() => morePages().loading),
    loadMoreError: computed(() => morePages().error),
    refresh: () => firstPageResource.reload(),
    loadMore,
  };
}

// Pages are cut newest first, so a ticket created between two requests pushes the last one of a page onto the next.
function withoutRepeats(tickets: readonly Ticket[]): readonly Ticket[] {
  const seen = new Set<string>();
  return tickets.filter((ticket) => {
    if (seen.has(ticket.id)) {
      return false;
    }
    seen.add(ticket.id);
    return true;
  });
}
