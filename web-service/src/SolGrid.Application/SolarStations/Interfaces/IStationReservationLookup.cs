/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IStationReservationLookup.cs
 * Description: Lets station management ask the reservation module about active bookings.
 * Contributor: Kavishi Godage
 */

namespace SolGrid.Application.SolarStations.Interfaces;

public interface IStationReservationLookup
{
    // Station deactivation must be blocked while bookings are still live, but reservation
    // state is owned by the reservation module. Station management depends on this contract
    // so it never reaches into another module's persistence.
    Task<int> CountActiveReservationsAsync(string stationId, CancellationToken cancellationToken = default);
}
