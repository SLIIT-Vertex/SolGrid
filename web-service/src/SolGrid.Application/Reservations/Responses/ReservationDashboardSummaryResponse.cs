/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationDashboardSummaryResponse.cs
 * Description: Returns server-calculated reservation dashboard counts.
 * Contributor: Dilshan Yapa S Y C T
 */

namespace SolGrid.Application.Reservations.Responses;

public sealed class ReservationDashboardSummaryResponse
{
    public long PendingReservationsCount { get; init; }

    public long ApprovedFutureReservationsCount { get; init; }

    public long CurrentReservationsCount { get; init; }

    public long BookingHistoryCount { get; init; }
}
