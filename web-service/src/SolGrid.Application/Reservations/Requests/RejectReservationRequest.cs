/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: RejectReservationRequest.cs
 * Description: Carries rejection data for a pending energy reservation.
 * Contributor: Dilshan Yapa S Y C T
 */

namespace SolGrid.Application.Reservations.Requests;

public sealed class RejectReservationRequest
{
    public string RejectionReason { get; init; } = string.Empty;
}
