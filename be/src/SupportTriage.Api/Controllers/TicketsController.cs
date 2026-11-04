using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportTriage.Api.Requests.Tickets;
using SupportTriage.Application.Common.Dto;
using SupportTriage.Application.Tickets.Commands;
using SupportTriage.Application.Tickets.Dto;
using SupportTriage.Application.Tickets.Queries;

namespace SupportTriage.Api.Controllers;

// No authentication by decision: the tenant comes only from the X-Tenant-Id header (TenantMiddleware).
[AllowAnonymous]
[Route(RoutePrefix)]
public sealed class TicketsController : ApiControllerBase
{
    private const string RoutePrefix = "api/v1/tickets";

    private const string ProblemJson = "application/problem+json";

    [EndpointSummary("Create ticket")]
    [EndpointDescription("Stores a support ticket for the tenant in the X-Tenant-Id header and returns 202 Accepted with status Pending at once; a background worker then classifies it (category and priority). Card numbers and security codes are masked before the ticket is stored. Fails with 400 when the email is missing or invalid, when both subject and body are empty, when a field is too long, or when the X-Tenant-Id header is missing or invalid.")]
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<TicketCreatedDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    public Task<IActionResult> CreateTicketAsync([FromBody] CreateTicketRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return AcceptedMediatorResponseAsync(
            new CreateTicketCommand(request.CustomerEmail, request.Subject, request.Body),
            static created => "/" + RoutePrefix + "/" + Uri.EscapeDataString(created.Id),
            cancellationToken);
    }

    [EndpointSummary("List tickets")]
    [EndpointDescription("Lists the tenant's tickets with their classification, newest first. Tickets that are still being triaged have status Pending or Processing and no category or priority yet; when an attempt failed, failureReason and failureMessage say why and that it will be retried. Tickets whose triage gave up (status Failed, isHidden true) are left out unless includeHidden is true, so an operator can find and classify them; their failureMessage explains why. category and priority can be repeated (category=Billing&category=Technical) and keep only tickets with one of the given values; tickets without a classification yet are left out by these filters. Values are the exact names Billing, Technical, Account, Shipping, Other and Low, Medium, High, Critical. Pass the returned continuationToken with the same filters to get the next page; pageSize defaults to 20 and is clamped to 1-100. Fails with 400 when a category or priority is not an allowed name, when the continuation token is not valid, or when the X-Tenant-Id header is missing or invalid.")]
    [HttpGet]
    [ProducesResponseType<ContinuationPageDto<TicketDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    public Task<IActionResult> GetTicketsAsync(
        [FromQuery] int? pageSize,
        [FromQuery] string? continuationToken,
        [FromQuery(Name = "category")] string[]? categories,
        [FromQuery(Name = "priority")] string[]? priorities,
        [FromQuery] bool includeHidden = false,
        CancellationToken cancellationToken = default)
    {
        return OkMediatorResponseAsync(new GetTicketsQuery(pageSize, continuationToken, includeHidden, categories, priorities), cancellationToken);
    }

    [EndpointSummary("Get ticket")]
    [EndpointDescription("Returns one of the tenant's tickets with its classification, and failureReason and failureMessage when triage failed. A ticket whose triage gave up (isHidden true) is returned only when includeHidden is true. Fails with 404 when the ticket does not exist in this tenant or is hidden and includeHidden is false, and with 400 when the X-Tenant-Id header is missing or invalid.")]
    [HttpGet("{ticketId:length(1,64)}")]
    [ProducesResponseType<TicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    public Task<IActionResult> GetTicketAsync([FromRoute] string ticketId, [FromQuery] bool includeHidden = false, CancellationToken cancellationToken = default)
    {
        return OkMediatorResponseAsync(new GetTicketQuery(ticketId, includeHidden), cancellationToken);
    }
}
