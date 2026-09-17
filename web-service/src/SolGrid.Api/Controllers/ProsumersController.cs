/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ProsumersController.cs
 * Description: Exposes public solar prosumer registration endpoints.
 * Contributor: Gunasekara H N
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolGrid.Api.Security;
using SolGrid.Application.Common.Models;
using SolGrid.Application.Auth.Interfaces;
using SolGrid.Application.Auth.Requests;
using SolGrid.Application.Auth.Responses;
using SolGrid.Application.Prosumers.Interfaces;
using SolGrid.Application.Prosumers.Requests;
using SolGrid.Application.Prosumers.Responses;
using SolGrid.Application.Users.Interfaces;

namespace SolGrid.Api.Controllers;

[ApiController]
[Route("api/v1/prosumers")]
public sealed class ProsumersController : ControllerBase
{
    private readonly IProsumerService prosumerService;
    private readonly IAuthService authService;

    public ProsumersController(IProsumerService prosumerService, IAuthService authService)
    {
        // Capture application services used by thin prosumer endpoints.
        this.prosumerService = prosumerService;
        this.authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProsumerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProsumerResponse>> Register(
        [FromBody] RegisterProsumerRequest request,
        CancellationToken cancellationToken)
    {
        // Register a pending prosumer profile without requiring an existing web-user JWT.
        var response = await prosumerService.RegisterProsumerAsync(request, cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1/prosumers/{response.Nic}", response);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Backoffice)]
    public async Task<ActionResult<ProsumerResponse>> Create(RegisterProsumerRequest request, CancellationToken cancellationToken)
    {
        // Create a pending profile for Backoffice using central registration validation.
        var response = await prosumerService.CreateProsumerAsync(request, cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1/prosumers/{response.Nic}", response);
    }

    [HttpPut("{nic}")]
    [Authorize(Policy = AuthorizationPolicies.Backoffice)]
    public async Task<ActionResult<ProsumerResponse>> Update(string nic, UpdateProsumerRequest request, CancellationToken cancellationToken)
    {
        // Maintain editable profile fields without changing NIC, password, or lifecycle status.
        return Ok(await prosumerService.UpdateProsumerAsync(nic, request, cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProsumerLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProsumerLoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        // Authenticate an active prosumer with the shared authentication service.
        var response = await authService.LoginProsumerAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ProsumerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProsumerResponse>> GetMyProfile(CancellationToken cancellationToken)
    {
        // Return the profile identified by the trusted authenticated prosumer subject.
        return Ok(await prosumerService.GetMyProsumerAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType(typeof(ProsumerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProsumerResponse>> UpdateMyProfile(UpdateProsumerRequest request, CancellationToken cancellationToken)
    {
        // Update only the authenticated prosumer's explicitly editable profile fields.
        return Ok(await prosumerService.UpdateMyProsumerAsync(request, cancellationToken).ConfigureAwait(false));
    }

    [HttpPatch("me/request-deactivation")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RequestDeactivation(CancellationToken cancellationToken)
    {
        // Record an authenticated prosumer's deactivation request without deleting history.
        await prosumerService.RequestMyDeactivationAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.Backoffice)]
    [ProducesResponseType(typeof(PagedResult<ProsumerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ProsumerResponse>>> GetProsumers(
        [FromQuery] string? searchText, [FromQuery] SolGrid.Domain.Enums.ProsumerAccountStatus? status,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        // Return a filtered Backoffice prosumer management page.
        return Ok(await prosumerService.GetProsumersAsync(new ProsumerQuery { SearchText = searchText, Status = status, PageNumber = pageNumber, PageSize = pageSize }, cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("pending")]
    [Authorize(Policy = AuthorizationPolicies.Backoffice)]
    [ProducesResponseType(typeof(PagedResult<ProsumerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ProsumerResponse>>> GetPending(CancellationToken cancellationToken)
    {
        // Return pending activations for the Backoffice web management workflow.
        return Ok(await prosumerService.GetProsumersAsync(new ProsumerQuery { Status = SolGrid.Domain.Enums.ProsumerAccountStatus.Pending }, cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("{nic}")]
    [Authorize(Policy = AuthorizationPolicies.Backoffice)]
    [ProducesResponseType(typeof(ProsumerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProsumerResponse>> GetByNic(string nic, CancellationToken cancellationToken)
    {
        // Return one prosumer profile for Backoffice management.
        return Ok(await prosumerService.GetProsumerByNicAsync(nic, cancellationToken).ConfigureAwait(false));
    }

    [HttpPatch("{nic}/activate")]
    [Authorize(Policy = AuthorizationPolicies.Backoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Activate(string nic, CancellationToken cancellationToken)
    {
        // Activate a pending prosumer through Backoffice management.
        await prosumerService.ActivateProsumerAsync(nic, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPatch("{nic}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.Backoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(string nic, CancellationToken cancellationToken)
    {
        // Deactivate a prosumer through Backoffice management.
        await prosumerService.DeactivateProsumerAsync(nic, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPatch("{nic}/reactivate")]
    [Authorize(Policy = AuthorizationPolicies.Backoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reactivate(string nic, CancellationToken cancellationToken)
    {
        // Reactivate a deactivated prosumer through Backoffice management.
        await prosumerService.ReactivateProsumerAsync(nic, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
