/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IReservationExpiryService.cs
 * Description: Defines server-side processing for overdue energy reservations.
 * Contributor: Dilshan Yapa S Y C T
 */

namespace SolGrid.Application.Reservations.Interfaces;

public interface IReservationExpiryService
{
    Task<long> ExpireDueReservationsAsync(CancellationToken cancellationToken = default);
}
