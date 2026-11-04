import { bindBoolean, bindOptionalInt } from './query-binding';

describe('bindOptionalInt', () => {
  it('binds a whole number', () => {
    expect(bindOptionalInt('25')).toEqual({ ok: true, value: 25 });
    expect(bindOptionalInt('-3')).toEqual({ ok: true, value: -3 });
  });

  it('treats a missing or empty value as not sent', () => {
    expect(bindOptionalInt(null)).toEqual({ ok: true, value: null });
    expect(bindOptionalInt('')).toEqual({ ok: true, value: null });
  });

  it.each(['abc', '2.5', '99999999999'])('rejects "%s" like model binding', (raw) => {
    expect(bindOptionalInt(raw)).toEqual({
      ok: false,
      message: `The value '${raw}' is not valid.`,
    });
  });
});

describe('bindBoolean', () => {
  it('binds true and false in any casing', () => {
    expect(bindBoolean('True', false)).toEqual({ ok: true, value: true });
    expect(bindBoolean('false', true)).toEqual({ ok: true, value: false });
  });

  it('falls back when the value is missing', () => {
    expect(bindBoolean(null, false)).toEqual({ ok: true, value: false });
  });

  it('rejects anything else', () => {
    expect(bindBoolean('yes', false)).toEqual({
      ok: false,
      message: "The value 'yes' is not valid.",
    });
  });
});
