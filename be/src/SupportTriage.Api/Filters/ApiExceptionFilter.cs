using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using SupportTriage.Api.Logging;
using SupportTriage.Domain.SharedKernel;

namespace SupportTriage.Api.Filters;

// Turns every exception from a controller into RFC 9457 problem details (api.md "Problem details").
internal sealed class ApiExceptionFilter : IExceptionFilter
{
    private const string ProblemJson = "application/problem+json";

    private const int FirstServerErrorStatus = 500;

    private readonly ProblemDetailsFactory _problemDetailsFactory;

    private readonly IHostEnvironment _environment;

    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ProblemDetailsFactory problemDetailsFactory, IHostEnvironment environment, ILogger<ApiExceptionFilter> logger)
    {
        _problemDetailsFactory = problemDetailsFactory ?? throw new ArgumentNullException(nameof(problemDetailsFactory));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void OnException(ExceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var httpContext = context.HttpContext;
        var path = httpContext.Request.Path.ToString();
        var exception = context.Exception;

        if (exception is not ExceptionBase known)
        {
            var incidentId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            if (httpContext.Response.HasStarted)
            {
                ApiLog.ResponseAlreadyStarted(_logger, exception, path, incidentId);
                return;
            }

            ApiLog.RequestFailedUnexpectedly(_logger, exception, path, incidentId);
            var unexpected = _problemDetailsFactory.CreateProblemDetails(httpContext, StatusCodes.Status500InternalServerError, detail: _environment.IsDevelopment() ? exception.ToString() : null, instance: path);
            unexpected.Extensions["incidentId"] = incidentId;
            SetResult(context, unexpected);
            return;
        }

        if (httpContext.Response.HasStarted)
        {
            ApiLog.ResponseAlreadyStarted(_logger, exception, path, known.IncidentId);
            return;
        }

        var status = (int)known.ErrorType;
        if (status >= FirstServerErrorStatus)
            ApiLog.RequestFailedUnexpectedly(_logger, exception, path, known.IncidentId);
        else
            ApiLog.RequestFailed(_logger, exception, path, status, known.IncidentId);

        var first = known.Errors[0];
        var problem = _problemDetailsFactory.CreateProblemDetails(httpContext, status, detail: first.UserFriendlyMessage ?? first.Message, instance: path);
        problem.Extensions["incidentId"] = known.IncidentId;

        var errors = new List<object>(known.Errors.Count);
        foreach (var error in known.Errors)
            errors.Add(new { code = error.Code, key = error.Key, message = error.Message });

        problem.Extensions["errors"] = errors;
        SetResult(context, problem);
    }

    private static void SetResult(ExceptionContext context, ProblemDetails problem)
    {
        var result = new ObjectResult(problem) { StatusCode = problem.Status };
        result.ContentTypes.Add(ProblemJson);
        context.Result = result;
        context.ExceptionHandled = true;
    }
}
