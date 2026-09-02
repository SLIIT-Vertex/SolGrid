/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: HttpCurrentUserContext.cs
 * Description: Reads the current API user identity from HTTP claims.
 * Contributor: Bawanthi K D R
 */

using SolGrid.Application.Common.Identity;
using SolGrid.Domain.Enums;
using System.Security.Claims;

namespace SolGrid.Api.Security;

public sealed class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        // Capture HTTP context access for API boundary identity mapping.
        this.httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public string? UserId => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public UserRole? Role
    {
        get
        {
            // Convert the role claim into the domain role enum when present.
            var roleClaim = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(roleClaim, out var role) ? role : null;
        }
    }
}
