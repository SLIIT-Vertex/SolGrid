/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: UsersController.cs
 * Description: Exposes Backoffice-only web user administration endpoints.
 * Contributor: Bawanthi K D R
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolGrid.Api.Security;
using SolGrid.Application.Common.Models;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Application.Users.Requests;
using SolGrid.Application.Users.Responses;
using SolGrid.Domain.Enums;

namespace SolGrid.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
[Route("api/v1/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService userService;

    public UsersController(IUserService userService)
    {
        // Capture the application service used by thin user administration endpoints.
        this.userService = userService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        // Create a web user after Backoffice authorization has succeeded.
        var response = await userService.CreateUserAsync(request, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<UserResponse>>> GetUsers(
        [FromQuery] string? searchText,
        [FromQuery] UserRole? role,
        [FromQuery] AccountStatus? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Return filtered and paged users for Backoffice administration.
        var response = await userService.GetUsersAsync(
            new UserQuery
            {
                SearchText = searchText,
                Role = role,
                Status = status,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken).ConfigureAwait(false);

        return Ok(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        // Return one web user by route id for Backoffice administration.
        var response = await userService.GetUserByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Update(
        string id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        // Update intended editable user fields after Backoffice authorization has succeeded.
        var response = await userService.UpdateUserAsync(id, request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpPatch("{id}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(string id, CancellationToken cancellationToken)
    {
        // Deactivate a web user account without physically deleting it.
        await userService.DeactivateUserAsync(id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPatch("{id}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(string id, CancellationToken cancellationToken)
    {
        // Reactivate a web user account and restore login eligibility.
        await userService.ReactivateUserAsync(id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
