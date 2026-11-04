import { HttpErrorResponse, HttpEvent, HttpResponse } from '@angular/common/http';
import { delay, dematerialize, materialize, Observable, of, throwError } from 'rxjs';
import { MockProblem } from './mock-problems';

// Long enough to see the loading states, short enough not to annoy.
export const readLatencyMs = 300;
export const createLatencyMs = 500;

export function respond(
  status: number,
  body: unknown,
  url: string,
  latencyMs: number,
): Observable<HttpEvent<unknown>> {
  return of(new HttpResponse({ status, body, url })).pipe(delay(latencyMs));
}

export function fail(
  status: number,
  problem: MockProblem,
  url: string,
  latencyMs: number,
): Observable<never> {
  const error = new HttpErrorResponse({ status, error: problem, url });
  // delay() lets errors through at once, so the error is materialized, delayed, then rethrown.
  return throwError(() => error).pipe(materialize(), delay(latencyMs), dematerialize());
}
