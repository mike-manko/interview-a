import { validateNewTicket } from './validate-new-ticket';

const valid = { customerEmail: 'a@example.com', subject: 'Parcel late', body: 'Where is it?' };

describe('validateNewTicket', () => {
  it('accepts a ticket with an email and a subject or a body', () => {
    expect(
      validateNewTicket({ customerEmail: 'a@example.com', subject: null, body: 'Hi' }),
    ).toEqual({ valid: true, ticket: { customerEmail: 'a@example.com', subject: '', body: 'Hi' } });
  });

  it('requires the email', () => {
    expect(validateNewTicket({ ...valid, customerEmail: '' })).toEqual({
      valid: false,
      errors: { CustomerEmail: ['The CustomerEmail field is required.'] },
    });
  });

  it.each(['nope', '@example.com', 'a@', 'a@b@c'])('rejects the email "%s"', (customerEmail) => {
    expect(validateNewTicket({ ...valid, customerEmail })).toEqual({
      valid: false,
      errors: { CustomerEmail: ['The CustomerEmail field is not a valid e-mail address.'] },
    });
  });

  it('rejects fields that are too long', () => {
    const result = validateNewTicket({
      customerEmail: `${'a'.repeat(250)}@example.com`,
      subject: 's'.repeat(201),
      body: 'b'.repeat(10_001),
    });

    expect(result).toEqual({
      valid: false,
      errors: {
        CustomerEmail: [
          "The field CustomerEmail must be a string or array type with a maximum length of '254'.",
        ],
        Subject: [
          "The field Subject must be a string or array type with a maximum length of '200'.",
        ],
        Body: ["The field Body must be a string or array type with a maximum length of '10000'."],
      },
    });
  });

  it('asks for a subject or a body when both are empty', () => {
    expect(validateNewTicket({ ...valid, subject: ' ', body: '' })).toEqual({
      valid: false,
      errors: { Body: ['Provide a subject or a body.'], Subject: ['Provide a subject or a body.'] },
    });
  });

  it('checks for a subject or a body only after the field rules pass', () => {
    expect(validateNewTicket({ customerEmail: 'nope', subject: '', body: '' })).toEqual({
      valid: false,
      errors: { CustomerEmail: ['The CustomerEmail field is not a valid e-mail address.'] },
    });
  });

  it('rejects a missing request body', () => {
    expect(validateNewTicket(null)).toMatchObject({ valid: false });
  });
});
