/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationsController.cs
 * Description: Exposes energy slot reservation endpoints.
 * Contributor: Dilshan Yapa
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Application.Reservations.Requests;
using SolGrid.Application.Reservations.Responses;

namespace SolGrid.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/reservations")]
public sealed class ReservationsController : ControllerBase
{
    private readonly IReservationService reservationService;

    public ReservationsController(IReservationService reservationService)
    {
        // Capture the application service used by thin reservation endpoints.
        this.reservationService = reservationService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> Create(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        // Create a pending energy reservation after authentication has succeeded.
        var response = await reservationService.CreateReservationAsync(request, cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1/reservations/{response.Id}", response);
    }
}
