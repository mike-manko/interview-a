using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace SupportTriage.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private IMediator Mediator => HttpContext.RequestServices.GetRequiredService<IMediator>();

    protected async Task<IActionResult> OkMediatorResponseAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        return Ok(await Mediator.Send(request, cancellationToken));
    }

    protected async Task<IActionResult> AcceptedMediatorResponseAsync<TResponse>(IRequest<TResponse> request, Func<TResponse, string> location, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(location);

        var response = await Mediator.Send(request, cancellationToken);

        return Accepted(location(response), response);
    }
}
