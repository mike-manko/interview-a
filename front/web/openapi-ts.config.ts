import { defineConfig } from '@hey-api/openapi-ts';

// The API serializes enums as strings (JsonStringEnumConverter on the MVC JSON options), but the OpenAPI generator
// reads the minimal-API JSON options, so the document describes every enum as a bare integer. These patches restore
// the names the API actually sends. Values mirror SupportTriage.Domain/TicketAggregate/Enums; remove the patches once
// the backend also registers the converter with ConfigureHttpJsonOptions.
const enumNames: Readonly<Record<string, readonly string[]>> = {
  TicketCategory: ['Billing', 'Technical', 'Account', 'Shipping', 'Other'],
  TicketPriority: ['Low', 'Medium', 'High', 'Critical'],
  TicketStatus: ['Pending', 'Processing', 'Completed', 'Failed'],
  TriageFailureReason: [
    'ProviderUnavailable',
    'InvalidResponse',
    'ProviderConfiguration',
    'Unexpected',
    'PromptInjection',
  ],
};

type PatchableSchema = Record<string, unknown>;

function toStringEnum(values: readonly string[]) {
  return (schema: PatchableSchema): void => {
    schema['type'] = 'string';
    schema['enum'] = [...values];
  };
}

// .NET 10 accepts numbers written as strings, so integers are documented as "integer | string"; responses are integers.
function toInteger(schema: PatchableSchema): void {
  schema['type'] = 'integer';
  delete schema['pattern'];
}

export default defineConfig({
  input: './openapi/support-triage.v1.json',
  output: { path: './src/app/api/generated' },
  parser: {
    patch: {
      schemas: {
        ...Object.fromEntries(
          Object.entries(enumNames).map(([name, values]) => [name, toStringEnum(values)]),
        ),
        TicketDto: (schema: PatchableSchema) => {
          const properties = schema['properties'] as Record<string, PatchableSchema>;
          toInteger(properties['failedTriageAttempts']);
        },
      },
    },
  },
  // Types only: the app calls the API through Angular's httpResource/HttpClient, not a generated SDK.
  plugins: [{ name: '@hey-api/typescript', enums: 'javascript' }],
});
