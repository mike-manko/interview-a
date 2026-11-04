# Product: Support triage

## What it is

An internal console for a support team that receives hundreds of customer tickets a day. An LLM, run by the .NET API's background worker, suggests a category, a priority and a draft reply for every ticket. Agents use the console to work the queue in priority order, check the AI's suggestion, and copy the draft reply.

This is an interview proof of concept. Authentication is a stand-in: the tenant is chosen in the header and sent as `X-Tenant-Id`.

## Who uses it

Support agents on long shifts at desks, on large screens in bright offices. They scan a queue many times an hour, open a ticket, read it, act, and move on. Speed of scanning beats discovery. A team lead also uses it to demo isolation between tenants.

## Jobs

1. See what needs doing first: the most urgent triaged tickets, and tickets the AI could not triage.
2. Narrow the queue by category and priority (server filters), and find a ticket by subject, email or id.
3. Open a ticket: read the customer's message, the AI's category and priority, and the suggested reply. Copy the reply.
4. Create a ticket from a phone call or email and watch it get classified.
5. Switch tenant and trust that nothing from the other tenant appears.

## Product truth (from the API)

- `GET /api/v1/tickets`: newest first, paged by continuation token. Filters: `category` and `priority` (repeatable), `includeHidden`. Unclassified tickets drop out as soon as a category or priority filter is set.
- `GET /api/v1/tickets/{id}`, `POST /api/v1/tickets` (202 Accepted; status Pending, classified later).
- Status: Pending, Processing, Completed, Failed. `failureReason` and `failureMessage` explain a failed attempt. Failed tickets are hidden (`isHidden`) unless `includeHidden` is set.
- There is no endpoint to trigger classification; the worker retries on its own.
- Customer text is untrusted: it contains HTML, prompt-injection attempts and masked card numbers. It is shown as plain text only.

## Tone

Plain, precise, calm. The product's own words: "triage", "queue", "suggested reply", "tenant". Errors say what happened and what the agent can do.

## Constraints

- Angular 22, Tailwind v4, the angular-dotnet house conventions.
- WCAG 2.2 AA; keyboard first.
- The AI's output is a suggestion: the suggested reply is always labelled as an AI draft to review.
