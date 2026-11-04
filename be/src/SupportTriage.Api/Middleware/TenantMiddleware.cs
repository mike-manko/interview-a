using Microsoft.AspNetCore.Mvc;
using SupportTriage.Api.Logging;
using SupportTriage.Application.Common.Interfaces;
using SupportTriage.Application.Common.Tenancy;

namespace SupportTriage.Api.Middleware;

// Assigns the request's tenant scope from X-Tenant-Id before any controller runs, so every repository and query below
// works inside that tenant's partition. There is no authentication by decision, so the header is trusted as sent.
internal sealed class TenantMiddleware
{
    public const string HeaderName = "X-Tenant-Id";

    private const string ApiPathPrefix = "/api";

    private readonly RequestDelegate _next;

    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, ITenantScope tenantScope, IProblemDetailsService problemDetailsService)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(tenantScope);
        ArgumentNullException.ThrowIfNull(problemDetailsService);

        if (!context.Request.Path.StartsWithSegments(ApiPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var values = context.Request.Headers[HeaderName];
        var tenantId = values.Count == 1 ? values[0] : null;
        if (!TenantIdFormat.IsValid(tenantId))
        {
            ApiLog.TenantHeaderRejected(_logger, context.Request.Method, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Missing or invalid tenant",
                    Detail = "Send exactly one X-Tenant-Id header with 1-64 lowercase letters, digits and dashes.",
                    Instance = context.Request.Path,
                },
            });
            return;
        }

        tenantScope.AssignScope(tenantId);
        await _next(context);
    }
}
