import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { problemDetailsInterceptor } from './problem-details-interceptor';

describe('problemDetailsInterceptor', () => {
  let http: HttpTestingController;
  let client: HttpClient;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([problemDetailsInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
  });

  afterEach(() => http.verify());

  it('turns a 400 ValidationProblemDetails into a validation error keyed by form field', async () => {
    const response = firstValueFrom(client.post('/api/tickets', {}));
    http.expectOne('/api/tickets').flush(
      {
        title: 'One or more validation errors occurred.',
        errors: { CustomerEmail: ['Required.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );

    await expect(response).rejects.toMatchObject({
      kind: 'validation',
      fieldErrors: { customerEmail: ['Required.'] },
    });
  });

  it('turns a 404 into a not-found error', async () => {
    const response = firstValueFrom(client.post('/api/tickets/t-404/classify', null));
    http
      .expectOne('/api/tickets/t-404/classify')
      .flush({ title: 'Not Found' }, { status: 404, statusText: 'Not Found' });

    await expect(response).rejects.toMatchObject({ kind: 'not-found' });
  });

  it('turns a failed connection into a network error', async () => {
    const response = firstValueFrom(client.get('/api/tickets'));
    http.expectOne('/api/tickets').error(new ProgressEvent('error'));

    await expect(response).rejects.toEqual({ kind: 'network' });
  });
});
