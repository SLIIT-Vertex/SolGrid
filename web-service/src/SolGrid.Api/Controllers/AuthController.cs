/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: AuthController.cs
 * Description: Exposes authentication endpoints for web users.
 * Contributor: Bawanthi K D R
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolGrid.Application.Auth.Interfaces;
using SolGrid.Application.Auth.Requests;
using SolGrid.Application.Auth.Responses;
using SolGrid.Application.Common.Identity;

namespace SolGrid.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService authService;
    private readonly ICurrentUserContext currentUserContext;

    public AuthController(IAuthService authService, ICurrentUserContext currentUserContext)
    {
        // Capture authentication services for thin endpoint actions.
        this.authService = authService;
        this.currentUserContext = currentUserContext;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        // Authenticate a web user and return a signed access token.
        var response = await authService.LoginAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<object> GetCurrentUser()
    {
        // Return the current authenticated identity from server-side JWT claims.
        return Ok(new
        {
            currentUserContext.UserId,
            Role = currentUserContext.Role?.ToString()
        });
    }

}
