/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: CreateBookingSlotRequest.cs
 * Description: Carries battery capacity and a booking interval for an owning station.
 * Contributor: Kavishi Godage
 */

namespace SolGrid.Application.SolarStations.Requests;

// Station identity comes from the owning station request or service route argument.
public sealed class CreateBookingSlotRequest
{
    public int SlotNumber { get; init; }

    public decimal BatteryCapacityKwh { get; init; }

    public DateTimeOffset StartTime { get; init; }

    public DateTimeOffset EndTime { get; init; }
}
