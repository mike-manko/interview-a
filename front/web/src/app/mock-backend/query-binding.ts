// ASP.NET Core model binding of simple query values: an empty or missing value means "not sent", and a value it cannot
// convert becomes a validation error with this message.
export type Bound<T> =
  { readonly ok: true; readonly value: T } | { readonly ok: false; readonly message: string };

const int32Pattern = /^\s*[+-]?\d+\s*$/;
const maxInt32 = 2_147_483_647;
const minInt32 = -2_147_483_648;

export function bindOptionalInt(raw: string | null): Bound<number | null> {
  if (raw === null || raw.trim() === '') {
    return { ok: true, value: null };
  }
  const value = Number(raw.trim());
  if (!int32Pattern.test(raw) || value > maxInt32 || value < minInt32) {
    return notValid(raw);
  }
  return { ok: true, value };
}

export function bindBoolean(raw: string | null, fallback: boolean): Bound<boolean> {
  if (raw === null || raw.trim() === '') {
    return { ok: true, value: fallback };
  }
  const normalized = raw.trim().toLowerCase();
  if (normalized === 'true') {
    return { ok: true, value: true };
  }
  if (normalized === 'false') {
    return { ok: true, value: false };
  }
  return notValid(raw);
}

function notValid(raw: string): Bound<never> {
  return { ok: false, message: `The value '${raw}' is not valid.` };
}
