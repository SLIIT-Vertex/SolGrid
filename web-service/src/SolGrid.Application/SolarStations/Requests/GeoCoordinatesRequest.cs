/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: GeoCoordinatesRequest.cs
 * Description: Carries a GPS position supplied by station management clients.
 * Contributor: Kavishi Godage
 */

namespace SolGrid.Application.SolarStations.Requests;

public sealed class GeoCoordinatesRequest
{
    public double Latitude { get; init; }

    public double Longitude { get; init; }
}
