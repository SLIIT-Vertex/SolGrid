/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ProsumersController.cs
 * Description: Exposes public solar prosumer registration endpoints.
 * Contributor: Gunasekara H N
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolGrid.Application.Prosumers.Interfaces;
using SolGrid.Application.Prosumers.Requests;
using SolGrid.Application.Prosumers.Responses;

namespace SolGrid.Api.Controllers;

[ApiController]
[Route("api/v1/prosumers")]
public sealed class ProsumersController : ControllerBase
{
    private readonly IProsumerService prosumerService;

    public ProsumersController(IProsumerService prosumerService)
    {
        // Capture the application service used by thin prosumer registration endpoints.
        this.prosumerService = prosumerService;
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
}
