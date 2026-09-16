/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: EnergyBookingSlotResponse.cs
 * Description: Returns battery storage slot state to API clients.
 * Contributor: Kavishi Godage
 */

using SolGrid.Domain.Enums;

namespace SolGrid.Application.SolarStations.Responses;

public sealed class EnergyBookingSlotResponse
{
    public string Id { get; init; } = string.Empty;

    public string StationId { get; init; } = string.Empty;

    public DateTimeOffset StartTime { get; init; }

    public DateTimeOffset EndTime { get; init; }

    public int SlotNumber { get; init; }

    public decimal BatteryCapacityKwh { get; init; }

    public SlotStatus Status { get; init; }

    public bool IsActive { get; init; }

    public bool IsAvailable { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
