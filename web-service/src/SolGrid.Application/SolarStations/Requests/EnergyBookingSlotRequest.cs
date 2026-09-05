/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: EnergyBookingSlotRequest.cs
 * Description: Carries battery storage slot hardware details for station configuration.
 * Contributor: Kavishi Godage
 */

namespace SolGrid.Application.SolarStations.Requests;

public sealed class EnergyBookingSlotRequest
{
    public int SlotNumber { get; init; }

    public decimal BatteryCapacityKwh { get; init; }
}
