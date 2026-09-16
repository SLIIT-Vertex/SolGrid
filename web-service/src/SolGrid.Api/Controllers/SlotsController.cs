/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: SlotsController.cs
 * Description: Exposes energy booking slot management endpoints.
 * Contributor: Kavishi Godage
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolGrid.Api.Security;
using SolGrid.Application.Common.Models;
using SolGrid.Application.SolarStations.Interfaces;
using SolGrid.Application.SolarStations.Requests;
using SolGrid.Application.SolarStations.Responses;
using SolGrid.Domain.Enums;

namespace SolGrid.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class SlotsController : ControllerBase
{
    private readonly IBookingSlotService bookingSlotService;

    public SlotsController(IBookingSlotService bookingSlotService)
    {
        // Capture the application service used by thin booking slot endpoints.
        this.bookingSlotService = bookingSlotService;
    }

    [HttpPost("stations/{stationId}/slots")]
    [Authorize(Policy = AuthorizationPolicies.Backoffice)]
    [ProducesResponseType(typeof(EnergyBookingSlotResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnergyBookingSlotResponse>> Create(
        string stationId,
        [FromBody] CreateBookingSlotRequest request,
        CancellationToken cancellationToken)
    {
        // Create a booking slot after Backoffice authorization has succeeded.
        var response = await bookingSlotService
            .CreateBookingSlotAsync(stationId, request, cancellationToken)
            .ConfigureAwait(false);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet("stations/{stationId}/slots")]
    [ProducesResponseType(typeof(PagedResult<EnergyBookingSlotResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<EnergyBookingSlotResponse>>> GetStationSlots(
        string stationId,
        [FromQuery] SlotStatus? status,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Return filtered and paged slots for one station to authenticated booking clients.
        var response = await bookingSlotService.GetBookingSlotsAsync(
            new BookingSlotQuery
            {
                StationId = stationId,
                Status = status,
                From = from,
                To = to,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken).ConfigureAwait(false);

        return Ok(response);
    }

    [HttpGet("slots/{id}")]
    [ProducesResponseType(typeof(EnergyBookingSlotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnergyBookingSlotResponse>> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        // Return one booking slot by route id for authenticated booking clients.
        var response = await bookingSlotService
            .GetBookingSlotByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);
        return Ok(response);
    }

    [HttpPut("slots/{id}")]
    [Authorize(Policy = AuthorizationPolicies.Backoffice)]
    [ProducesResponseType(typeof(EnergyBookingSlotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnergyBookingSlotResponse>> Update(
        string id,
        [FromBody] UpdateBookingSlotRequest request,
        CancellationToken cancellationToken)
    {
        // Update editable slot details after Backoffice authorization has succeeded.
        var response = await bookingSlotService
            .UpdateBookingSlotAsync(id, request, cancellationToken)
            .ConfigureAwait(false);
        return Ok(response);
    }

    [HttpPatch("slots/{id}/activate")]
    [Authorize(Policy = AuthorizationPolicies.Backoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Activate(string id, CancellationToken cancellationToken)
    {
        // Activate a booking slot through Backoffice management.
        await bookingSlotService.ActivateBookingSlotAsync(id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPatch("slots/{id}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.Backoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(string id, CancellationToken cancellationToken)
    {
        // Deactivate a booking slot without deleting historical reservation references.
        await bookingSlotService.DeactivateBookingSlotAsync(id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
