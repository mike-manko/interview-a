import { HttpErrorResponse } from '@angular/common/http';

// Mirrors ASP.NET Core ProblemDetails / ValidationProblemDetails (RFC 9457).
export interface ProblemDetails {
  readonly type?: string;
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;
  readonly instance?: string;
  readonly traceId?: string;
  // Set by the API on handled errors; support can find the logs by it.
  readonly incidentId?: string;
}

export type FieldErrors = Readonly<Record<string, readonly string[]>>;

export type ApiError =
  | {
      readonly kind: 'validation';
      readonly fieldErrors: FieldErrors;
      readonly problem: ProblemDetails;
    }
  | { readonly kind: 'conflict'; readonly problem: ProblemDetails }
  | { readonly kind: 'unauthorized'; readonly problem: ProblemDetails }
  | { readonly kind: 'not-found'; readonly problem: ProblemDetails }
  | { readonly kind: 'server'; readonly problem: ProblemDetails }
  | { readonly kind: 'network' };

export function toApiError(response: HttpErrorResponse): ApiError {
  if (response.status === 0) {
    return { kind: 'network' };
  }
  const problem = toProblemDetails(response);
  const fieldErrors = response.status === 400 ? toFieldErrors(response.error) : undefined;
  if (fieldErrors) {
    return { kind: 'validation', fieldErrors, problem };
  }
  if (response.status === 401 || response.status === 403) {
    return { kind: 'unauthorized', problem };
  }
  if (response.status === 404) {
    return { kind: 'not-found', problem };
  }
  // 412 is the If-Match precondition failure: someone else changed the record.
  if (response.status === 412 || response.status === 409) {
    return { kind: 'conflict', problem };
  }
  return { kind: 'server', problem };
}

// "Address.City" or "address.City" becomes "address.city": the API's JSON naming policy decides the casing.
export function toFieldName(propertyPath: string): string {
  return propertyPath
    .split('.')
    .map((segment) => segment.charAt(0).toLowerCase() + segment.slice(1))
    .join('.');
}

function toProblemDetails(response: HttpErrorResponse): ProblemDetails {
  const body: unknown = response.error;
  if (!isRecord(body)) {
    return { status: response.status, title: response.statusText };
  }
  return {
    type: optionalString(body['type']),
    title: optionalString(body['title']) ?? response.statusText,
    status: typeof body['status'] === 'number' ? body['status'] : response.status,
    detail: optionalString(body['detail']),
    instance: optionalString(body['instance']),
    traceId: optionalString(body['traceId']),
    incidentId: optionalString(body['incidentId']),
  };
}

// Model validation sends ValidationProblemDetails (errors as a field-to-messages dictionary); domain validation sends
// errors as a list of { code, key, message }. Both become the same field-to-messages map.
function toFieldErrors(body: unknown): FieldErrors | undefined {
  if (!isRecord(body)) {
    return undefined;
  }
  const errors = body['errors'];
  if (Array.isArray(errors)) {
    return fromErrorList(errors);
  }
  if (!isRecord(errors)) {
    return undefined;
  }
  const entries = Object.entries(errors).flatMap(([key, messages]) =>
    isStringArray(messages) ? [[toFieldName(key), messages] as const] : [],
  );
  return Object.fromEntries(entries);
}

function fromErrorList(errors: readonly unknown[]): FieldErrors {
  const fieldErrors: Record<string, string[]> = {};
  for (const error of errors) {
    if (!isRecord(error)) {
      continue;
    }
    const key = optionalString(error['key']);
    const message = optionalString(error['message']);
    if (key === undefined || message === undefined) {
      continue;
    }
    const field = toFieldName(key);
    fieldErrors[field] = [...(fieldErrors[field] ?? []), message];
  }
  return fieldErrors;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isStringArray(value: unknown): value is readonly string[] {
  return Array.isArray(value) && value.every((item) => typeof item === 'string');
}

function optionalString(value: unknown): string | undefined {
  return typeof value === 'string' ? value : undefined;
}

const apiErrorKinds: readonly string[] = [
  'validation',
  'conflict',
  'unauthorized',
  'not-found',
  'server',
  'network',
];

// problemDetailsInterceptor rethrows every HttpErrorResponse as an ApiError; this tells callers which one they caught.
export function isApiError(value: unknown): value is ApiError {
  return (
    isRecord(value) && typeof value['kind'] === 'string' && apiErrorKinds.includes(value['kind'])
  );
}

// httpResource wraps a thrown value that is not an Error (our ApiError) in a ResourceWrappedError and keeps the
// original as `cause`, so resource.error() needs unwrapping before callers can tell which ApiError it was.
export function findApiError(error: unknown): ApiError | undefined {
  if (isApiError(error)) {
    return error;
  }
  if (error instanceof Error && isApiError(error.cause)) {
    return error.cause;
  }
  return undefined;
}
