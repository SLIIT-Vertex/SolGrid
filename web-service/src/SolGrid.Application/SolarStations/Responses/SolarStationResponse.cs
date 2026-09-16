/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: SolarStationResponse.cs
 * Description: Returns microgrid solar station details to API clients.
 * Contributor: Kavishi Godage
 */

using SolGrid.Domain.Enums;

namespace SolGrid.Application.SolarStations.Responses;

public sealed class SolarStationResponse
{
    public string Id { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string AddressLine { get; init; } = string.Empty;

    public GeoCoordinatesResponse Location { get; init; } = new();

    public decimal CapacityKw { get; init; }

    public StationStatus Status { get; init; }

    public int TotalSlotCount { get; init; }

    public int AvailableSlotCount { get; init; }

    public IReadOnlyList<EnergyBookingSlotResponse> Slots { get; init; } = Array.Empty<EnergyBookingSlotResponse>();

    public IReadOnlyList<OperatingWindowResponse> Schedule { get; init; } = Array.Empty<OperatingWindowResponse>();

    public double? DistanceKilometers { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
