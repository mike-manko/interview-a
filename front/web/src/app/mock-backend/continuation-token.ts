// Where the previous page ended in the newest-first order. Keyed on the last ticket rather than an offset, so tickets
// created or triaged between two pages neither repeat nor go missing.
export interface TicketKey {
  readonly createdAt: number;
  readonly id: string;
}

// Opaque to the client, like the API's base64url-encoded Cosmos token.
export function encodeContinuationToken(key: TicketKey): string {
  const base64 = btoa(encodeURIComponent(JSON.stringify(key)));
  return base64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

// Undefined when the token was not made by encodeContinuationToken.
export function decodeContinuationToken(token: string): TicketKey | undefined {
  const base64 = token.replace(/-/g, '+').replace(/_/g, '/');
  try {
    const parsed: unknown = JSON.parse(decodeURIComponent(atob(base64)));
    return isTicketKey(parsed) ? parsed : undefined;
  } catch {
    return undefined;
  }
}

function isTicketKey(value: unknown): value is TicketKey {
  if (typeof value !== 'object' || value === null) {
    return false;
  }
  const candidate: Readonly<Record<string, unknown>> = { ...value };
  return (
    typeof candidate['createdAt'] === 'number' &&
    Number.isFinite(candidate['createdAt']) &&
    typeof candidate['id'] === 'string'
  );
}
