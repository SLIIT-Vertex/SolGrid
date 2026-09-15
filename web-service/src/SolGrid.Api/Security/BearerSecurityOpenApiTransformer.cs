/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: BearerSecurityOpenApiTransformer.cs
 * Description: Adds JWT bearer authentication metadata to the OpenAPI document.
 * Contributor: Bawanthi K D R
 */

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SolGrid.Api.Security;

public sealed class BearerSecurityOpenApiTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Advertise JWT bearer authentication in generated OpenAPI metadata.
        var components = document.Components ??= new OpenApiComponents();
        components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter a valid JWT bearer token."
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });

        return Task.CompletedTask;
    }
}
