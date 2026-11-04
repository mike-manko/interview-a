import { Routes } from '@angular/router';

export default [
  {
    path: 'new',
    title: 'New ticket',
    loadComponent: () => import('./ticket-create-page').then((m) => m.TicketCreatePage),
  },
  {
    path: '',
    title: 'Tickets',
    loadComponent: () => import('./tickets-page').then((m) => m.TicketsPage),
    children: [
      {
        path: ':id',
        title: 'Ticket',
        loadComponent: () => import('./ticket-detail-page').then((m) => m.TicketDetailPage),
      },
    ],
  },
] satisfies Routes;
