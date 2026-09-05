/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: OperatingWindowRequest.cs
 * Description: Carries one weekday availability window supplied by station management clients.
 * Contributor: Kavishi Godage
 */

namespace SolGrid.Application.SolarStations.Requests;

public sealed class OperatingWindowRequest
{
    public DayOfWeek Day { get; init; }

    public TimeOnly OpensAt { get; init; }

    public TimeOnly ClosesAt { get; init; }
}
