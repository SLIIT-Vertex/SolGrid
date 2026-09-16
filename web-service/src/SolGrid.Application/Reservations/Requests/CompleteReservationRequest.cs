/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: CompleteReservationRequest.cs
 * Description: Carries completion data for an approved energy reservation.
 * Contributor: Dilshan Yapa S Y C T
 */

namespace SolGrid.Application.Reservations.Requests;

public sealed class CompleteReservationRequest
{
    public string VerificationToken { get; init; } = string.Empty;
}
