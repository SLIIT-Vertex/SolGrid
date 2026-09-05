/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: CreateSolarStationRequest.cs
 * Description: Carries the details required to register a new microgrid solar station.
 * Contributor: Kavishi Godage
 */

namespace SolGrid.Application.SolarStations.Requests;

public sealed class CreateSolarStationRequest
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string AddressLine { get; init; } = string.Empty;

    public GeoCoordinatesRequest? Location { get; init; }

    public decimal CapacityKw { get; init; }

    public IReadOnlyList<EnergyBookingSlotRequest> Slots { get; init; } = Array.Empty<EnergyBookingSlotRequest>();

    public IReadOnlyList<OperatingWindowRequest> Schedule { get; init; } = Array.Empty<OperatingWindowRequest>();
}
