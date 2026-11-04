import {
  badRequestProblem,
  tenantProblem,
  ticketNotFoundProblem,
  validationProblem,
} from './mock-problems';

describe('mock problems', () => {
  it('describes a missing tenant like TenantMiddleware', () => {
    expect(tenantProblem('/api/v1/tickets')).toMatchObject({
      title: 'Missing or invalid tenant',
      status: 400,
      instance: '/api/v1/tickets',
      traceId: expect.stringMatching(/^00-[0-9a-f]{32}-[0-9a-f]{16}-00$/),
    });
  });

  it('carries model validation errors as a dictionary', () => {
    const errors = { CustomerEmail: ['The CustomerEmail field is required.'] };

    expect(validationProblem(errors)).toMatchObject({
      title: 'One or more validation errors occurred.',
      status: 400,
      errors,
    });
  });

  it('carries domain errors as a list, with the first message as detail', () => {
    const errors = [
      { code: 2203, key: 'category', message: 'first' },
      { code: 2204, key: 'priority', message: 'second' },
    ];

    expect(badRequestProblem(errors, '/api/v1/tickets')).toMatchObject({
      title: 'Bad Request',
      status: 400,
      detail: 'first',
      errors,
      incidentId: expect.stringMatching(/^[0-9a-f]{32}$/),
    });
  });

  it('reports a missing ticket as 404 with the API error code', () => {
    expect(ticketNotFoundProblem('t-9', '/api/v1/tickets/t-9')).toMatchObject({
      status: 404,
      detail: 'Ticket t-9 has not been found.',
      errors: [{ code: 1015, key: 't-9', message: 'Ticket t-9 has not been found.' }],
    });
  });
});
