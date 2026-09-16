/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationStationReadService.cs
 * Description: Adapts solar station data to the reservation component's focused read contract.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Application.SolarStations.Interfaces;

namespace SolGrid.Application.SolarStations.Services;

public sealed class ReservationStationReadService : IReservationStationReadService
{
    private readonly ISolarStationRepository stationRepository;

    public ReservationStationReadService(ISolarStationRepository stationRepository)
    {
        // Capture the station abstraction without exposing persistence implementation details.
        this.stationRepository = stationRepository;
    }

    public async Task<ReservationStationSnapshot?> GetByIdAsync(
        string stationId,
        CancellationToken cancellationToken = default)
    {
        // Return only the identity and eligibility snapshot required by reservations.
        if (string.IsNullOrWhiteSpace(stationId))
        {
            return null;
        }

        var station = await stationRepository
            .GetByIdAsync(stationId.Trim(), cancellationToken)
            .ConfigureAwait(false);

        return station is null
            ? null
            : new ReservationStationSnapshot
            {
                Id = station.Id,
                IsActive = station.IsActive
            };
    }
}
