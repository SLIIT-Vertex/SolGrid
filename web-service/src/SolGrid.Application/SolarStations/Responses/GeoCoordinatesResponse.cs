/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: GeoCoordinatesResponse.cs
 * Description: Returns a solar station GPS position to API clients.
 * Contributor: Kavishi Godage
 */

namespace SolGrid.Application.SolarStations.Responses;

public sealed class GeoCoordinatesResponse
{
    public double Latitude { get; init; }

    public double Longitude { get; init; }
}
