import {
  TicketCategory,
  ticketCategories,
  TicketPriority,
  ticketPriorities,
} from '../tickets/ticket';
import { decodeContinuationToken, TicketKey } from './continuation-token';
import { DomainError, ValidationErrors } from './mock-problems';
import { bindBoolean, bindOptionalInt } from './query-binding';

export interface TicketListQuery {
  readonly pageSize: number;
  // Null for the first page.
  readonly after: TicketKey | null;
  readonly categories: readonly TicketCategory[];
  readonly priorities: readonly TicketPriority[];
  readonly includeHidden: boolean;
}

// The three ways GET /api/v1/tickets answers its query string, in the order the API checks them: model binding
// (ValidationProblemDetails), then the filter names, then the continuation token (both domain-style errors).
export type ParsedTicketListQuery =
  | { readonly kind: 'query'; readonly query: TicketListQuery }
  | { readonly kind: 'binding-errors'; readonly errors: ValidationErrors }
  | { readonly kind: 'domain-errors'; readonly errors: readonly DomainError[] };

export const defaultPageSize = 20;
export const maxPageSize = 100;

export function parseTicketListQuery(params: URLSearchParams): ParsedTicketListQuery {
  const pageSize = bindOptionalInt(params.get('pageSize'));
  const includeHidden = bindBoolean(params.get('includeHidden'), false);
  if (!pageSize.ok || !includeHidden.ok) {
    const errors: Record<string, readonly string[]> = {};
    if (!pageSize.ok) {
      errors['pageSize'] = [pageSize.message];
    }
    if (!includeHidden.ok) {
      errors['includeHidden'] = [includeHidden.message];
    }
    return { kind: 'binding-errors', errors };
  }

  const categoryValues = params.getAll('category');
  const priorityValues = params.getAll('priority');
  const filterErrors = [
    ...categoryValues.filter((value) => !isCategory(value)).map(categoryError),
    ...priorityValues.filter((value) => !isPriority(value)).map(priorityError),
  ];
  if (filterErrors.length > 0) {
    return { kind: 'domain-errors', errors: filterErrors };
  }

  const token = params.get('continuationToken') ?? '';
  const after = token === '' ? null : decodeContinuationToken(token);
  if (after === undefined) {
    return { kind: 'domain-errors', errors: [continuationTokenError] };
  }

  return {
    kind: 'query',
    query: {
      pageSize: clampPageSize(pageSize.value),
      after,
      categories: categoryValues.filter(isCategory),
      priorities: priorityValues.filter(isPriority),
      includeHidden: includeHidden.value,
    },
  };
}

function clampPageSize(pageSize: number | null): number {
  if (pageSize === null) {
    return defaultPageSize;
  }
  return Math.min(Math.max(pageSize, 1), maxPageSize);
}

// Exact, case-sensitive names, like the API's TriageValueParser: "billing" and " Billing" are not allowed.
function isCategory(value: string): value is TicketCategory {
  return ticketCategories.some((category) => category === value);
}

function isPriority(value: string): value is TicketPriority {
  return ticketPriorities.some((priority) => priority === value);
}

function categoryError(value: string): DomainError {
  return {
    code: 2203,
    key: 'category',
    message: `Category filter '${value}' is not one of: Billing, Technical, Account, Shipping, Other.`,
  };
}

function priorityError(value: string): DomainError {
  return {
    code: 2204,
    key: 'priority',
    message: `Priority filter '${value}' is not one of: Low, Medium, High, Critical.`,
  };
}

const continuationTokenError: DomainError = {
  code: 4001,
  key: 'continuationToken',
  message: 'The continuation token is not valid; start again from the first page.',
};
