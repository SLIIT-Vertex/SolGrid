/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: VerifyReservationQrRequest.cs
 * Description: Carries QR verification data for an energy reservation.
 * Contributor: Dilshan Yapa S Y C T
 */

namespace SolGrid.Application.Reservations.Requests;

public sealed class VerifyReservationQrRequest
{
    public string ReservationId { get; init; } = string.Empty;

    public string VerificationToken { get; init; } = string.Empty;
}
