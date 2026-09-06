/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationStationLookup.cs
 * Description: Adapts reservation persistence so station deactivation can detect live bookings.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Application.SolarStations.Interfaces;

namespace SolGrid.Application.SolarStations.Services;

public sealed class ReservationStationLookup : IStationReservationLookup
{
    private readonly IReservationRepository reservationRepository;

    public ReservationStationLookup(IReservationRepository reservationRepository)
    {
        // Capture the reservation abstraction without reaching into reservation persistence.
        this.reservationRepository = reservationRepository;
    }

    public async Task<int> CountActiveReservationsAsync(
        string stationId,
        CancellationToken cancellationToken = default)
    {
        // Translate the reservation module's existence check into the station deactivation count.
        if (string.IsNullOrWhiteSpace(stationId))
        {
            return 0;
        }

        var hasActiveReservations = await reservationRepository
            .HasActiveReservationsForStationAsync(stationId.Trim(), cancellationToken)
            .ConfigureAwait(false);

        return hasActiveReservations ? 1 : 0;
    }

    public async Task<bool> HasActiveReservationForSlotAsync(
        string bookingSlotId,
        CancellationToken cancellationToken = default)
    {
        // Translate the reservation module's slot occupancy check for slot lifecycle operations.
        if (string.IsNullOrWhiteSpace(bookingSlotId))
        {
            return false;
        }

        return await reservationRepository
            .HasActiveReservationForBookingSlotAsync(bookingSlotId.Trim(), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
