/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: VerifyReservationQrResponse.cs
 * Description: Returns QR verification result data for an energy reservation.
 * Contributor: Dilshan Yapa S Y C T
 */

namespace SolGrid.Application.Reservations.Responses;

public sealed class VerifyReservationQrResponse
{
    public bool IsValid { get; init; }

    public string ReservationId { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}
