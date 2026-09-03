/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: JwtBearerOptionsSetup.cs
 * Description: Configures JWT bearer validation from typed API configuration.
 * Contributor: Bawanthi K D R
 */

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SolGrid.Infrastructure.Security;
using System.Security.Claims;
using System.Text;

namespace SolGrid.Api.Security;

public sealed class JwtBearerOptionsSetup : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly IOptions<JwtOptions> jwtOptions;

    public JwtBearerOptionsSetup(IOptions<JwtOptions> jwtOptions)
    {
        // Capture typed JWT options after host configuration is finalized.
        this.jwtOptions = jwtOptions;
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        // Configure the default JWT bearer scheme only.
        if (name != JwtBearerDefaults.AuthenticationScheme)
        {
            return;
        }

        Configure(options);
    }

    public void Configure(JwtBearerOptions options)
    {
        // Validate JWT access tokens using typed configuration values.
        var values = jwtOptions.Value;
        ValidateOptions(values);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = values.Issuer,
            ValidateAudience = true,
            ValidAudience = values.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(values.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = HandleChallengeAsync,
            OnForbidden = HandleForbiddenAsync
        };
    }

    private static void ValidateOptions(JwtOptions options)
    {
        // Ensure bearer authentication cannot run with missing or weak JWT configuration.
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            throw new InvalidOperationException("JWT issuer is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("JWT audience is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.SigningKey) || Encoding.UTF8.GetByteCount(options.SigningKey) < 32)
        {
            throw new InvalidOperationException("JWT signing key must be at least 32 bytes.");
        }
    }

    private static async Task HandleChallengeAsync(JwtBearerChallengeContext context)
    {
        // Return a consistent problem response for missing or invalid JWT access tokens.
        context.HandleResponse();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Authentication is required."
        }).ConfigureAwait(false);
    }

    private static async Task HandleForbiddenAsync(ForbiddenContext context)
    {
        // Return a consistent problem response for authenticated users without required roles.
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Access is forbidden."
        }).ConfigureAwait(false);
    }
}
