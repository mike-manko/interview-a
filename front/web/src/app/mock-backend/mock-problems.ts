// Error bodies shaped like the API's: TenantMiddleware's tenant problem, ASP.NET Core's ValidationProblemDetails from
// model validation, and ApiExceptionFilter's domain problems (errors as a { code, key, message } list).
export type MockProblem = Readonly<Record<string, unknown>>;

export type ValidationErrors = Readonly<Record<string, readonly string[]>>;

export interface DomainError {
  readonly code: number;
  readonly key: string;
  readonly message: string;
}

const badRequestType = 'https://tools.ietf.org/html/rfc9110#section-15.5.1';
const notFoundType = 'https://tools.ietf.org/html/rfc9110#section-15.5.5';
const methodNotAllowedType = 'https://tools.ietf.org/html/rfc9110#section-15.5.6';

export function tenantProblem(path: string): MockProblem {
  return {
    type: badRequestType,
    title: 'Missing or invalid tenant',
    status: 400,
    detail: 'Send exactly one X-Tenant-Id header with 1-64 lowercase letters, digits and dashes.',
    instance: path,
    traceId: newTraceId(),
  };
}

export function validationProblem(errors: ValidationErrors): MockProblem {
  return {
    type: badRequestType,
    title: 'One or more validation errors occurred.',
    status: 400,
    errors,
    traceId: newTraceId(),
  };
}

export function badRequestProblem(errors: readonly DomainError[], path: string): MockProblem {
  return domainProblem(400, 'Bad Request', badRequestType, errors, path);
}

export function ticketNotFoundProblem(ticketId: string, path: string): MockProblem {
  const error: DomainError = {
    code: 1015,
    key: ticketId,
    message: `Ticket ${ticketId} has not been found.`,
  };
  return domainProblem(404, 'Not Found', notFoundType, [error], path);
}

export function routeNotFoundProblem(path: string): MockProblem {
  return { type: notFoundType, title: 'Not Found', status: 404, instance: path };
}

export function methodNotAllowedProblem(path: string): MockProblem {
  return { type: methodNotAllowedType, title: 'Method Not Allowed', status: 405, instance: path };
}

function domainProblem(
  status: number,
  title: string,
  type: string,
  errors: readonly DomainError[],
  path: string,
): MockProblem {
  return {
    type,
    title,
    status,
    detail: errors[0]?.message,
    instance: path,
    incidentId: randomHex(32),
    errors,
    traceId: newTraceId(),
  };
}

// W3C trace context format, as ASP.NET Core writes it into problem details.
function newTraceId(): string {
  return `00-${randomHex(32)}-${randomHex(16)}-00`;
}

function randomHex(length: number): string {
  const bytes = crypto.getRandomValues(new Uint8Array(length / 2));
  return Array.from(bytes, (byte) => byte.toString(16).padStart(2, '0')).join('');
}
