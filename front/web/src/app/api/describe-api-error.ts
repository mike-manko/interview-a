import { findApiError } from './problem-details';

// One user-facing sentence per ApiError kind (services.md "Errors in the UI").
// Accepts resource.error() too: findApiError unwraps the ResourceWrappedError around it.
export function describeApiError(value: unknown): string {
  const error = findApiError(value);
  if (error === undefined) {
    return 'Something went wrong. Please try again.';
  }
  switch (error.kind) {
    case 'network':
      return 'Cannot reach the server. Check your connection and try again.';
    case 'validation':
      return 'Some fields are not valid. Correct them and try again.';
    case 'unauthorized':
      return 'You are not allowed to do this.';
    case 'not-found':
      return 'This ticket does not exist in the selected tenant.';
    case 'conflict':
      return 'Someone else changed this ticket. Reload and try again.';
    case 'server':
      return withTraceId(
        error.problem.title ?? 'The server could not complete the request.',
        error.problem.incidentId ?? error.problem.traceId,
      );
    default:
      return error satisfies never;
  }
}

// The incident id is what the API logs handled errors under; the trace id is the fallback for everything else.
function withTraceId(message: string, reference: string | undefined): string {
  if (reference === undefined) {
    return message;
  }
  return `${message} (support reference: ${reference})`;
}
