/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationsController.cs
 * Description: Exposes energy slot reservation endpoints.
 * Contributor: Dilshan Yapa
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolGrid.Application.Common.Models;
using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Application.Reservations.Requests;
using SolGrid.Application.Reservations.Responses;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Domain.Enums;

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

    [HttpGet]
    [Authorize(Roles = "Backoffice,GridOperator")]
    [ProducesResponseType(typeof(PagedResult<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ReservationResponse>>> GetReservations(
        [FromQuery] string? prosumerId,
        [FromQuery] string? stationId,
        [FromQuery] string? bookingSlotId,
        [FromQuery] ReservationStatus? status,
        [FromQuery] string? searchText,
        [FromQuery] DateTimeOffset? scheduledFrom,
        [FromQuery] DateTimeOffset? scheduledTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Return filtered and paged reservations for operational web roles.
        var response = await reservationService.GetReservationsAsync(
            new ReservationQuery
            {
                ProsumerId = prosumerId,
                StationId = stationId,
                BookingSlotId = bookingSlotId,
                Status = status,
                SearchText = searchText,
                ScheduledFrom = scheduledFrom,
                ScheduledTo = scheduledTo,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken).ConfigureAwait(false);

        return Ok(response);
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(PagedResult<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ReservationResponse>>> GetMyReservations(
        [FromQuery] string? stationId,
        [FromQuery] string? bookingSlotId,
        [FromQuery] ReservationStatus? status,
        [FromQuery] string? searchText,
        [FromQuery] DateTimeOffset? scheduledFrom,
        [FromQuery] DateTimeOffset? scheduledTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Return filtered and paged reservations owned by the current caller.
        var response = await reservationService.GetMyReservationsAsync(
            new ReservationQuery
            {
                StationId = stationId,
                BookingSlotId = bookingSlotId,
                Status = status,
                SearchText = searchText,
                ScheduledFrom = scheduledFrom,
                ScheduledTo = scheduledTo,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken).ConfigureAwait(false);

        return Ok(response);
    }

    [HttpGet("me/dashboard/summary")]
    public async Task<ActionResult<ReservationDashboardSummaryResponse>> GetMyDashboardSummary(CancellationToken cancellationToken = default)
    {
        // Return owner-scoped counts; the application service rejects web-user identities.
        return Ok(await reservationService.GetMyDashboardSummaryAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("dashboard/summary")]
    [Authorize(Roles = "Backoffice,GridOperator")]
    [ProducesResponseType(typeof(ReservationDashboardSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ReservationDashboardSummaryResponse>> GetDashboardSummary(
        CancellationToken cancellationToken = default)
    {
        // Return server-calculated counts for the operational reservation dashboard.
        var response = await reservationService.GetDashboardSummaryAsync(cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpGet("current")]
    [Authorize(Roles = "Backoffice,GridOperator")]
    [ProducesResponseType(typeof(PagedResult<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ReservationResponse>>> GetCurrentReservations(
        [FromQuery] string? prosumerId,
        [FromQuery] string? stationId,
        [FromQuery] string? bookingSlotId,
        [FromQuery] string? searchText,
        [FromQuery] DateTimeOffset? scheduledFrom,
        [FromQuery] DateTimeOffset? scheduledTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Return upcoming approved reservations through a bounded server-side page.
        var response = await reservationService.GetDashboardReservationsAsync(
            ReservationDashboardView.Current,
            BuildQuery(prosumerId, stationId, bookingSlotId, null, searchText, scheduledFrom, scheduledTo, pageNumber, pageSize),
            cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Backoffice,GridOperator")]
    [ProducesResponseType(typeof(PagedResult<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ReservationResponse>>> GetPendingReservations(
        [FromQuery] string? prosumerId,
        [FromQuery] string? stationId,
        [FromQuery] string? bookingSlotId,
        [FromQuery] string? searchText,
        [FromQuery] DateTimeOffset? scheduledFrom,
        [FromQuery] DateTimeOffset? scheduledTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Return pending reservations through a bounded server-side page.
        var response = await reservationService.GetDashboardReservationsAsync(
            ReservationDashboardView.Pending,
            BuildQuery(prosumerId, stationId, bookingSlotId, null, searchText, scheduledFrom, scheduledTo, pageNumber, pageSize),
            cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpGet("history")]
    [Authorize(Roles = "Backoffice,GridOperator")]
    [ProducesResponseType(typeof(PagedResult<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ReservationResponse>>> GetReservationHistory(
        [FromQuery] string? prosumerId,
        [FromQuery] string? stationId,
        [FromQuery] string? bookingSlotId,
        [FromQuery] ReservationStatus? status,
        [FromQuery] string? searchText,
        [FromQuery] DateTimeOffset? scheduledFrom,
        [FromQuery] DateTimeOffset? scheduledTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Return historical reservations through a bounded server-side page.
        var response = await reservationService.GetDashboardReservationsAsync(
            ReservationDashboardView.History,
            BuildQuery(prosumerId, stationId, bookingSlotId, status, searchText, scheduledFrom, scheduledTo, pageNumber, pageSize),
            cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        // Return one reservation when the caller is allowed to access it.
        var response = await reservationService.GetReservationByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    private static ReservationQuery BuildQuery(
        string? prosumerId,
        string? stationId,
        string? bookingSlotId,
        ReservationStatus? status,
        string? searchText,
        DateTimeOffset? scheduledFrom,
        DateTimeOffset? scheduledTo,
        int pageNumber,
        int pageSize)
    {
        // Build an application query from documented dashboard query-string parameters.
        return new ReservationQuery
        {
            ProsumerId = prosumerId,
            StationId = stationId,
            BookingSlotId = bookingSlotId,
            Status = status,
            SearchText = searchText,
            ScheduledFrom = scheduledFrom,
            ScheduledTo = scheduledTo,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
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

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> Update(
        string id,
        [FromBody] UpdateReservationRequest request,
        CancellationToken cancellationToken)
    {
        // Update an active reservation after ownership and notice checks succeed.
        var response = await reservationService.UpdateReservationAsync(id, request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpPatch("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(string id, CancellationToken cancellationToken)
    {
        // Cancel an active reservation without deleting its historical record.
        await reservationService.CancelReservationAsync(id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPatch("{id}/approve")]
    [Authorize(Roles = "Backoffice")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> Approve(
        string id,
        [FromBody] ApproveReservationRequest request,
        CancellationToken cancellationToken)
    {
        // Approve a pending reservation using the authenticated reviewer identity.
        var response = await reservationService.ApproveReservationAsync(id, request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpPatch("{id}/reject")]
    [Authorize(Roles = "Backoffice")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> Reject(
        string id,
        [FromBody] RejectReservationRequest request,
        CancellationToken cancellationToken)
    {
        // Reject a pending reservation using the authenticated reviewer identity.
        var response = await reservationService.RejectReservationAsync(id, request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpPost("{id}/qr")]
    [ProducesResponseType(typeof(ReservationQrResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationQrResponse>> IssueQr(
        string id,
        CancellationToken cancellationToken)
    {
        // Issue a short-lived QR transaction token for an approved reservation.
        var response = await reservationService.IssueReservationQrAsync(id, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpPost("verify-qr")]
    [Authorize(Roles = "Backoffice")]
    [ProducesResponseType(typeof(VerifyReservationQrResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VerifyReservationQrResponse>> VerifyQr(
        [FromBody] VerifyReservationQrRequest request,
        CancellationToken cancellationToken)
    {
        // Verify a QR token against server-side reservation transaction state.
        var response = await reservationService.VerifyReservationQrAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpPost("{id}/complete")]
    [Authorize(Roles = "Backoffice")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> Complete(
        string id,
        [FromBody] CompleteReservationRequest request,
        CancellationToken cancellationToken)
    {
        // Complete an approved reservation after server-side QR token revalidation.
        var response = await reservationService.CompleteReservationAsync(id, request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }
}
