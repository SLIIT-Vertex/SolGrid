/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: UpdateReservationRequest.cs
 * Description: Carries editable energy reservation schedule fields.
 * Contributor: Dilshan Yapa S Y C T
 */

namespace SolGrid.Application.Reservations.Requests;

public sealed class UpdateReservationRequest
{
    public string StationId { get; init; } = string.Empty;

    public string BookingSlotId { get; init; } = string.Empty;

    public DateTimeOffset ScheduledAt { get; init; }
}
