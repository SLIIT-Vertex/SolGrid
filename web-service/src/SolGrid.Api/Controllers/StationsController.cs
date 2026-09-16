/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: StationsController.cs
 * Description: Exposes microgrid solar station management endpoints.
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
[Route("api/v1/stations")]
public sealed class StationsController : ControllerBase
{
    private readonly ISolarStationService stationService;

    public StationsController(ISolarStationService stationService)
    {
        // Capture the application service used by thin station management endpoints.
        this.stationService = stationService;
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Backoffice)]
    [ProducesResponseType(typeof(SolarStationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SolarStationResponse>> Create(
        [FromBody] CreateSolarStationRequest request,
        CancellationToken cancellationToken)
    {
        // Create a solar station after Backoffice authorization has succeeded.
        var response = await stationService.CreateStationAsync(request, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet]
    [Authorize(Roles = "Backoffice,GridOperator")]
    [ProducesResponseType(typeof(PagedResult<SolarStationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<SolarStationResponse>>> GetStations(
        [FromQuery] StationStatus? status,
        [FromQuery] string? searchText,
        [FromQuery] bool? hasAvailableSlots,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Return filtered and paged stations for operational web roles.
        var response = await stationService.GetStationsAsync(
            new SolarStationQuery
            {
                Status = status,
                SearchText = searchText,
                HasAvailableSlots = hasAvailableSlots,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken).ConfigureAwait(false);

        return Ok(response);
    }

    [HttpGet("nearby")]
    [ProducesResponseType(typeof(IReadOnlyList<SolarStationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<SolarStationResponse>>> GetNearby(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double radiusKilometers = 10d,
        [FromQuery] int maxResults = 20,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        // Return stations near a client GPS origin so Android Maps never stores authoritative node data.
        var response = await stationService.GetNearbyStationsAsync(
            new NearbyStationQuery
            {
                Latitude = latitude,
                Longitude = longitude,
                RadiusKilometers = radiusKilometers,
                MaxResults = maxResults,
                ActiveOnly = activeOnly
            },
            cancellationToken).ConfigureAwait(false);

        return Ok(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SolarStationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SolarStationResponse>> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        // Return one solar station by route id, including coordinates used by Android Maps.
        var response = await stationService.GetStationByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.Backoffice)]
    [ProducesResponseType(typeof(SolarStationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SolarStationResponse>> Update(
        string id,
        [FromBody] UpdateSolarStationRequest request,
        CancellationToken cancellationToken)
    {
        // Update editable station details after Backoffice authorization has succeeded.
        var response = await stationService.UpdateStationAsync(id, request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpPut("{id}/schedule")]
    [Authorize(Policy = AuthorizationPolicies.Backoffice)]
    [ProducesResponseType(typeof(SolarStationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SolarStationResponse>> ReplaceSchedule(
        string id,
        [FromBody] UpdateStationScheduleRequest request,
        CancellationToken cancellationToken)
    {
        // Replace the weekly operating schedule after Backoffice authorization has succeeded.
        var response = await stationService.ReplaceScheduleAsync(id, request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpPatch("{id}/activate")]
    [Authorize(Policy = AuthorizationPolicies.Backoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(string id, CancellationToken cancellationToken)
    {
        // Activate a solar station through Backoffice management.
        await stationService.ActivateStationAsync(id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPatch("{id}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.Backoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(string id, CancellationToken cancellationToken)
    {
        // Deactivate a solar station through Backoffice management when no live bookings exist.
        await stationService.DeactivateStationAsync(id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
