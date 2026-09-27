/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationExpiryPolicy.cs
 * Description: Defines centralized reservation expiry timings.
 * Contributor: Dilshan Yapa S Y C T
 */

namespace SolGrid.Application.Reservations.Services;

public static class ReservationExpiryPolicy
{
    public static readonly TimeSpan WorkerInterval = TimeSpan.FromMinutes(5);

    public static readonly TimeSpan ApprovedCompletionGracePeriod = TimeSpan.FromMinutes(30);
}
