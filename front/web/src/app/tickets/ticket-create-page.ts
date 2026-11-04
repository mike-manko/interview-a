import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import {
  email,
  FieldTree,
  form,
  FormField,
  FormRoot,
  maxLength,
  required,
  TreeValidationResult,
  validate,
  ValidationError,
} from '@angular/forms/signals';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { describeApiError } from '../api/describe-api-error';
import { FieldErrors, isApiError } from '../api/problem-details';
import { FieldError } from '../shared/field-error/field-error';
import { TenantContext } from '../tenancy/tenant-context';
import { NewTicket } from './ticket';
import { TicketsApi } from './tickets-api';

// NewTicket allows a missing or null subject and body; form fields need a string, so the form edits this shape.
interface TicketDraft {
  readonly customerEmail: string;
  readonly subject: string;
  readonly body: string;
}

const emptyTicket: TicketDraft = { customerEmail: '', subject: '', body: '' };

// The API's limits (CreateTicketRequest); checking them here saves a round trip.
const customerEmailMaxLength = 254;
const subjectMaxLength = 200;
const bodyMaxLength = 10000;
// The counter appears only near the limit, where it helps; below that it would be noise.
const bodyCounterThreshold = 9000;

// Same words as the API's own rule, so the message does not change when the server answers instead.
const subjectOrBodyMessage = 'Provide a subject or a body.';

@Component({
  selector: 'app-ticket-create-page',
  imports: [DecimalPipe, FieldError, FormField, FormRoot, RouterLink],
  templateUrl: './ticket-create-page.html',
})
export class TicketCreatePage {
  private readonly api = inject(TicketsApi);
  private readonly router = inject(Router);
  private readonly tenant = inject(TenantContext);

  protected readonly bodyMaxLength = bodyMaxLength;
  protected readonly tenantId = this.tenant.current;
  protected readonly submitError = signal<string | null>(null);
  protected readonly ticketForm = form(
    signal<TicketDraft>(emptyTicket),
    (path) => {
      required(path.customerEmail, { message: 'Enter the customer email.' });
      email(path.customerEmail, { message: 'Enter a valid email address.' });
      maxLength(path.customerEmail, customerEmailMaxLength, {
        message: `The email can have at most ${customerEmailMaxLength} characters.`,
      });
      maxLength(path.subject, subjectMaxLength, {
        message: `The subject can have at most ${subjectMaxLength} characters.`,
      });
      maxLength(path.body, bodyMaxLength, {
        message: 'The message can have at most 10,000 characters.',
      });
      // Either field satisfies the rule, so both carry the error and both clear once one is filled in.
      validate(path.subject, ({ valueOf }) =>
        subjectOrBodyError(valueOf(path.subject), valueOf(path.body)),
      );
      validate(path.body, ({ valueOf }) =>
        subjectOrBodyError(valueOf(path.subject), valueOf(path.body)),
      );
    },
    { submission: { action: (field) => this.createTicket(field) } },
  );

  protected readonly bodyLength = computed(() => this.ticketForm.body().value().length);
  protected readonly showsBodyCounter = computed(() => this.bodyLength() > bodyCounterThreshold);
  protected readonly bodyCounterClass = computed(() => {
    const color = this.bodyLength() > bodyMaxLength ? 'text-danger' : 'text-text-muted';
    return `shrink-0 text-xs tabular-nums ${color}`;
  });
  // Server errors for fields the form does not have land on the form itself.
  protected readonly formErrors = computed(() =>
    this.ticketForm()
      .errors()
      .map((error) => error.message ?? 'The ticket is not valid.'),
  );

  protected showsError(field: FieldTree<string>): boolean {
    return field().touched() && field().invalid();
  }

  private async createTicket(field: FieldTree<TicketDraft>): Promise<TreeValidationResult> {
    this.submitError.set(null);
    try {
      const ticket: NewTicket = field().value();
      const created = await firstValueFrom(this.api.createTicket(ticket));
      // The detail panel polls while the worker classifies, so the agent watches it get triaged.
      await this.router.navigate(['/tickets', created.id]);
      return undefined;
    } catch (error) {
      if (isApiError(error) && error.kind === 'validation') {
        return toServerErrors(field, error.fieldErrors);
      }
      this.submitError.set(describeApiError(error));
      return undefined;
    }
  }
}

function subjectOrBodyError(subject: string, body: string): ValidationError | undefined {
  if (subject.trim() !== '' || body.trim() !== '') {
    return undefined;
  }
  return { kind: 'subjectOrBody', message: subjectOrBodyMessage };
}

// Puts each server message on its field; a key the form does not know lands on the form itself.
function toServerErrors(
  field: FieldTree<TicketDraft>,
  fieldErrors: FieldErrors,
): TreeValidationResult {
  return Object.entries(fieldErrors).map(([name, messages]) => ({
    kind: 'server',
    message: messages.join(' '),
    fieldTree: isDraftField(name) ? field[name] : field,
  }));
}

function isDraftField(name: string): name is keyof TicketDraft {
  return Object.keys(emptyTicket).includes(name);
}
