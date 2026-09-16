/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: UpdateBookingSlotRequest.cs
 * Description: Carries editable capacity and booking times without changing slot identity.
 * Contributor: Kavishi Godage
 */

namespace SolGrid.Application.SolarStations.Requests;

public sealed class UpdateBookingSlotRequest
{
    public decimal BatteryCapacityKwh { get; init; }

    public DateTimeOffset StartTime { get; init; }

    public DateTimeOffset EndTime { get; init; }
}
