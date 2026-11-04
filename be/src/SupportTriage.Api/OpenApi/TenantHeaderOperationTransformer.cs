using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using SupportTriage.Api.Middleware;

namespace SupportTriage.Api.OpenApi;

// Every business endpoint needs the tenant header, so it is documented once here instead of on each action.
internal sealed class TenantHeaderOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = TenantMiddleware.HeaderName,
            In = ParameterLocation.Header,
            Required = true,
            Description = "Tenant the request works in: 1-64 lowercase letters, digits and dashes (for example contoso).",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String },
        });

        return Task.CompletedTask;
    }
}
