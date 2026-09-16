/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: CreateReservationRequest.cs
 * Description: Carries data required to request an energy slot reservation.
 * Contributor: Dilshan Yapa S Y C T
 */

namespace SolGrid.Application.Reservations.Requests;

public sealed class CreateReservationRequest
{
    public string ProsumerId { get; init; } = string.Empty;

    public string StationId { get; init; } = string.Empty;

    public string BookingSlotId { get; init; } = string.Empty;

    public DateTimeOffset ScheduledAt { get; init; }
}
