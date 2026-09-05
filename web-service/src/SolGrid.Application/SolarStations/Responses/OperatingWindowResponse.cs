/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: OperatingWindowResponse.cs
 * Description: Returns one weekday availability window to API clients.
 * Contributor: Kavishi Godage
 */

namespace SolGrid.Application.SolarStations.Responses;

public sealed class OperatingWindowResponse
{
    public DayOfWeek Day { get; init; }

    public TimeOnly OpensAt { get; init; }

    public TimeOnly ClosesAt { get; init; }
}
