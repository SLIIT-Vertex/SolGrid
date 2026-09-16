/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationQrResponse.cs
 * Description: Returns a short-lived QR transaction payload for approved reservations.
 * Contributor: Dilshan Yapa S Y C T
 */

namespace SolGrid.Application.Reservations.Responses;

public sealed class ReservationQrResponse
{
    public string ReservationId { get; init; } = string.Empty;

    public string VerificationToken { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; init; }
}
