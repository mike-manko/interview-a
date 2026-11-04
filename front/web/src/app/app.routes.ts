import { Routes } from '@angular/router';

export const routes = [
  { path: '', pathMatch: 'full', redirectTo: 'tickets' },
  { path: 'tickets', loadChildren: () => import('./tickets/tickets.routes') },
  { path: '**', redirectTo: 'tickets' },
] satisfies Routes;
