import { HttpClient, HttpHeaders, httpResource, HttpResourceRef } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { TenantContext } from '../tenancy/tenant-context';
import { CreatedTicket, NewTicket, Ticket, TicketPage } from './ticket';
import { TicketQuery, toTicketParams } from './ticket-query';

export const ticketsUrl = '/api/v1/tickets';
export const tenantHeader = 'X-Tenant-Id';

// Every request carries the selected tenant. The resources read it inside their request function, so switching
// tenants cancels the old request and loads the new tenant's data; nothing from another tenant is ever kept.
@Service()
export class TicketsApi {
  private readonly http = inject(HttpClient);
  private readonly tenant = inject(TenantContext);

  /** First page for the query; re-requests when the query or the tenant changes. Call in an injection context. */
  firstPage(query: () => TicketQuery): HttpResourceRef<TicketPage | undefined> {
    return httpResource<TicketPage>(() => ({
      url: ticketsUrl,
      params: toTicketParams(query()),
      headers: this.tenantHeaders(),
    }));
  }

  /** The page after continuationToken, for "Load more". Must use the same query as the first page. */
  nextPage(query: TicketQuery, continuationToken: string): Observable<TicketPage> {
    return this.http.get<TicketPage>(ticketsUrl, {
      params: toTicketParams(query, continuationToken),
      headers: this.tenantHeaders(),
    });
  }

  /**
   * One ticket, hidden ones included so a ticket whose triage gave up still opens.
   * Idle while id() is undefined. Call in an injection context.
   */
  ticket(id: () => string | undefined): HttpResourceRef<Ticket | undefined> {
    return httpResource<Ticket>(() => {
      const ticketId = id();
      if (ticketId === undefined) {
        return undefined;
      }
      return {
        url: `${ticketsUrl}/${encodeURIComponent(ticketId)}`,
        params: { includeHidden: 'true' },
        headers: this.tenantHeaders(),
      };
    });
  }

  /** 202 Accepted: the ticket is stored with status Pending and classified later by the API's worker. */
  createTicket(ticket: NewTicket): Observable<CreatedTicket> {
    return this.http.post<CreatedTicket>(ticketsUrl, ticket, { headers: this.tenantHeaders() });
  }

  private tenantHeaders(): HttpHeaders {
    return new HttpHeaders({ [tenantHeader]: this.tenant.current() });
  }
}
