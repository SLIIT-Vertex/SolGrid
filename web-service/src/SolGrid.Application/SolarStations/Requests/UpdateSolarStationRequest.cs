/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: UpdateSolarStationRequest.cs
 * Description: Carries the editable details of an existing microgrid solar station.
 * Contributor: Kavishi Godage
 */

namespace SolGrid.Application.SolarStations.Requests;

public sealed class UpdateSolarStationRequest
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string AddressLine { get; init; } = string.Empty;

    public GeoCoordinatesRequest? Location { get; init; }

    public decimal CapacityKw { get; init; }
}
