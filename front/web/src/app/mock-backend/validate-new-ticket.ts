import { ValidationErrors } from './mock-problems';

// A create request that passed validation; a missing subject or body is stored as an empty string.
export interface ValidNewTicket {
  readonly customerEmail: string;
  readonly subject: string;
  readonly body: string;
}

export type NewTicketValidation =
  | { readonly valid: true; readonly ticket: ValidNewTicket }
  | { readonly valid: false; readonly errors: ValidationErrors };

export const maxCustomerEmailLength = 254;
export const maxSubjectLength = 200;
export const maxBodyLength = 10_000;

// Mirrors the API's CreateTicketRequest data annotations, with ASP.NET Core's messages and PascalCase keys. As in
// ASP.NET Core, the "subject or body" rule (IValidatableObject) only runs once every field rule has passed.
export function validateNewTicket(requestBody: unknown): NewTicketValidation {
  if (typeof requestBody !== 'object' || requestBody === null) {
    return { valid: false, errors: { '': ['A non-empty request body is required.'] } };
  }
  const fields: Readonly<Record<string, unknown>> = { ...requestBody };
  const customerEmail = optionalString(fields['customerEmail']);
  const subject = optionalString(fields['subject']);
  const body = optionalString(fields['body']);

  const errors: Record<string, readonly string[]> = {};
  const emailErrors = customerEmailErrors(customerEmail);
  if (emailErrors.length > 0) {
    errors['CustomerEmail'] = emailErrors;
  }
  if (subject !== null && subject.length > maxSubjectLength) {
    errors['Subject'] = [tooLong('Subject', maxSubjectLength)];
  }
  if (body !== null && body.length > maxBodyLength) {
    errors['Body'] = [tooLong('Body', maxBodyLength)];
  }
  if (Object.keys(errors).length > 0) {
    return { valid: false, errors };
  }

  if (isBlank(subject) && isBlank(body)) {
    const message = 'Provide a subject or a body.';
    return { valid: false, errors: { Body: [message], Subject: [message] } };
  }
  return {
    valid: true,
    ticket: { customerEmail: customerEmail ?? '', subject: subject ?? '', body: body ?? '' },
  };
}

// [Required] stops at its own failure, so an empty email is only "required"; the other rules each add a message.
function customerEmailErrors(customerEmail: string | null): readonly string[] {
  if (customerEmail === null || customerEmail.trim() === '') {
    return ['The CustomerEmail field is required.'];
  }
  const errors: string[] = [];
  if (!isEmailAddress(customerEmail)) {
    errors.push('The CustomerEmail field is not a valid e-mail address.');
  }
  if (customerEmail.length > maxCustomerEmailLength) {
    errors.push(tooLong('CustomerEmail', maxCustomerEmailLength));
  }
  return errors;
}

// .NET's EmailAddressAttribute: exactly one "@", neither first nor last, and no line breaks.
function isEmailAddress(value: string): boolean {
  const at = value.indexOf('@');
  return (
    at > 0 &&
    at !== value.length - 1 &&
    at === value.lastIndexOf('@') &&
    !value.includes('\r') &&
    !value.includes('\n')
  );
}

function tooLong(field: string, maxLength: number): string {
  return `The field ${field} must be a string or array type with a maximum length of '${maxLength}'.`;
}

function isBlank(value: string | null): boolean {
  return value === null || value.trim() === '';
}

function optionalString(value: unknown): string | null {
  return typeof value === 'string' ? value : null;
}
