import { HttpErrorResponse } from '@angular/common/http';
import { findApiError, isApiError, toApiError, toFieldName } from './problem-details';

describe('problem details', () => {
  it('maps .NET property paths to camelCase form field names', () => {
    expect(toFieldName('Address.City')).toBe('address.city');
  });

  it('keeps the traceId of a server error for support', () => {
    const error = toApiError(
      new HttpErrorResponse({ status: 500, error: { title: 'Boom', traceId: '00-abc' } }),
    );

    expect(error).toEqual({
      kind: 'server',
      problem: {
        type: undefined,
        title: 'Boom',
        status: 500,
        detail: undefined,
        instance: undefined,
        traceId: '00-abc',
        incidentId: undefined,
      },
    });
  });

  it('maps the model-validation dictionary to field errors', () => {
    const error = toApiError(
      new HttpErrorResponse({
        status: 400,
        error: {
          errors: { CustomerEmail: ['The CustomerEmail field is not a valid e-mail address.'] },
        },
      }),
    );

    expect(error).toMatchObject({
      kind: 'validation',
      fieldErrors: { customerEmail: ['The CustomerEmail field is not a valid e-mail address.'] },
    });
  });

  it('maps the domain error list to field errors and keeps the incident id', () => {
    const error = toApiError(
      new HttpErrorResponse({
        status: 400,
        error: {
          detail: "Category filter 'tech' is not allowed.",
          incidentId: 'f62b',
          errors: [
            { code: 2203, key: 'category', message: 'Not allowed.' },
            { code: 2204, key: 'Category', message: 'Still not allowed.' },
            { code: 1, message: 'No key, so no field.' },
          ],
        },
      }),
    );

    expect(error).toMatchObject({
      kind: 'validation',
      fieldErrors: { category: ['Not allowed.', 'Still not allowed.'] },
      problem: { incidentId: 'f62b' },
    });
  });

  it('recognises an ApiError and rejects other values', () => {
    expect(isApiError({ kind: 'network' })).toBe(true);
    expect(isApiError({ kind: 'teapot' })).toBe(false);
    expect(isApiError(new Error('x'))).toBe(false);
  });

  it('finds an ApiError directly or as the cause of a wrapping error', () => {
    const notFound = { kind: 'not-found', problem: {} } as const;

    expect(findApiError(notFound)).toBe(notFound);
    expect(findApiError(new Error('wrapped', { cause: notFound }))).toBe(notFound);
    expect(findApiError(new Error('plain'))).toBeUndefined();
  });
});
