/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationDashboardView.cs
 * Description: Defines server-side reservation dashboard views.
 * Contributor: Dilshan Yapa S Y C T
 */

namespace SolGrid.Application.Reservations.Interfaces;

public enum ReservationDashboardView
{
    Current,
    Pending,
    History
}

public sealed class ReservationDashboardCounts
{
    public long PendingReservationsCount { get; init; }

    public long ApprovedFutureReservationsCount { get; init; }

    public long CurrentReservationsCount { get; init; }

    public long BookingHistoryCount { get; init; }
}
