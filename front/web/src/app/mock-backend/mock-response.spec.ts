import { HttpErrorResponse, HttpEvent, HttpResponse } from '@angular/common/http';
import { firstValueFrom, Observable } from 'rxjs';
import { fail, respond } from './mock-response';

describe('mock responses', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  async function settle<T>(response: Observable<HttpEvent<T>>, latencyMs: number) {
    const settled = firstValueFrom(response).then(
      (event) => event,
      (error: unknown) => error,
    );
    await vi.advanceTimersByTimeAsync(latencyMs);
    return settled;
  }

  it('answers after the latency', async () => {
    let answered = false;
    const response = respond(202, { id: 'mock-1' }, '/api/v1/tickets', 500);
    response.subscribe(() => (answered = true));

    await vi.advanceTimersByTimeAsync(499);
    expect(answered).toBe(false);

    const event = await settle(response, 500);
    expect(event).toBeInstanceOf(HttpResponse);
    expect(event).toMatchObject({ status: 202, body: { id: 'mock-1' } });
  });

  it('fails after the latency too, with the problem as the error body', async () => {
    const error = await settle(fail(404, { status: 404 }, '/api/v1/tickets/x', 300), 300);

    expect(error).toBeInstanceOf(HttpErrorResponse);
    expect(error).toMatchObject({ status: 404, error: { status: 404 } });
  });
});
