/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: UpdateStationScheduleRequest.cs
 * Description: Carries a replacement weekly availability schedule for a solar station.
 * Contributor: Kavishi Godage
 */

namespace SolGrid.Application.SolarStations.Requests;

public sealed class UpdateStationScheduleRequest
{
    public IReadOnlyList<OperatingWindowRequest> Schedule { get; init; } = Array.Empty<OperatingWindowRequest>();
}
