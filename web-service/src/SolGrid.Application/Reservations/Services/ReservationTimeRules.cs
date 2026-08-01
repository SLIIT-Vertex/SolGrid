/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationTimeRules.cs
 * Description: Centralizes scheduling window rules for energy reservations.
 * Contributor: Dilshan Yapa S Y C T
 */

namespace SolGrid.Application.Reservations.Services;

public static class ReservationTimeRules
{
    public const int MaximumAdvanceReservationDays = 7;

    public static DateTimeOffset NormalizeToUtc(DateTimeOffset timestamp)
    {
        // Convert client-provided timestamps to UTC before applying reservation rules.
        return timestamp.ToUniversalTime();
    }

    public static bool IsScheduledInPast(DateTimeOffset scheduledAtUtc, DateTimeOffset nowUtc)
    {
        // Check whether the requested schedule is before the current UTC time.
        return scheduledAtUtc < nowUtc;
    }

    public static bool IsBeyondMaximumReservationWindow(DateTimeOffset scheduledAtUtc, DateTimeOffset nowUtc)
    {
        // Check whether the requested schedule exceeds the assignment reservation window.
        return scheduledAtUtc > nowUtc.AddDays(MaximumAdvanceReservationDays);
    }
}
