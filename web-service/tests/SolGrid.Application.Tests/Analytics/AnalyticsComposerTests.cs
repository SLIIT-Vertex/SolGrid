/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: AnalyticsComposerTests.cs
 * Description: Verifies node, reservation, and business-rule analytics.
 * Contributor: Dilshan Yapa
 */

using SolGrid.Application.Analytics.Services;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using SolGrid.Domain.ValueObjects;
using Xunit;

namespace SolGrid.Application.Tests.Analytics;

public class AnalyticsComposerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Compose_flags_bookings_outside_the_seven_day_window_and_blocks_deactivation()
    {
        // A live booking more than seven days ahead is a breach, and it blocks node deactivation.
        var reserved = EnergyBookingSlot.Restore(
            "slot-held", "station-1", 1, 20m,
            Now.AddDays(1), Now.AddDays(1).AddHours(1),
            SlotStatus.Reserved, Now, Now);
        var free = EnergyBookingSlot.Create(
            "slot-free", "station-1", 2, 10m,
            Now.AddDays(2), Now.AddDays(2).AddHours(1), Now);
        var station = SolarStation.Create(
            "station-1", "HUB-1", "Kandy Hub", "Kandy",
            GeoCoordinates.Create(7.29, 80.63), 40m,
            [reserved, free],
            [OperatingWindow.Create(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0))],
            Now);
        var inside = EnergyReservation.Create("res-1", "199012345678", station.Id, reserved.Id, Now.AddDays(1), Now);
        var beyond = EnergyReservation.Create("res-2", "199012345678", station.Id, free.Id, Now.AddDays(10), Now);

        var snapshot = AnalyticsComposer.Compose([station], [inside, beyond], null, null, Now, includeAccounts: false);

        Assert.Equal(2, snapshot.Reservations.Total);
        Assert.Equal(1, snapshot.Reservations.BeyondSevenDayWindow);
        Assert.Equal(1, snapshot.Reservations.InsideSevenDayWindow);
        var node = Assert.Single(snapshot.Nodes);
        Assert.True(node.DeactivationBlocked);
        Assert.Equal(2, node.ActiveReservations);
        Assert.Equal(BusinessRoutineOutcome.Breach, Routine(snapshot, "seven-day-window"));
        Assert.Equal(BusinessRoutineOutcome.Attention, Routine(snapshot, "node-deactivation"));
        Assert.Equal(BusinessRoutineOutcome.Clear, Routine(snapshot, "one-reservation-per-slot"));
        Assert.Null(snapshot.Accounts);
    }

    [Fact]
    public void Compose_marks_a_slot_with_two_live_reservations_as_a_breach()
    {
        // Two pending reservations on one slot violate the single-booking rule.
        var slot = EnergyBookingSlot.Create(
            "slot-1", "station-1", 1, 15m, Now.AddHours(20), Now.AddHours(21), Now);
        var station = SolarStation.Create(
            "station-1", "HUB-1", "Galle Hub", "Galle",
            GeoCoordinates.Create(6.05, 80.22), 25m,
            [slot],
            [OperatingWindow.Create(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(16, 0))],
            Now);
        var first = EnergyReservation.Create("res-1", "199012345678", station.Id, slot.Id, Now.AddHours(20), Now);
        var second = EnergyReservation.Create("res-2", "200012345678", station.Id, slot.Id, Now.AddHours(20), Now);

        var snapshot = AnalyticsComposer.Compose([station], [first, second], null, null, Now, includeAccounts: false);

        Assert.Equal(BusinessRoutineOutcome.Breach, Routine(snapshot, "one-reservation-per-slot"));
        Assert.Equal(1, snapshot.Nodes[0].SlotStateDrift);
    }

    private static BusinessRoutineOutcome Routine(
        SolGrid.Application.Analytics.Responses.AnalyticsSnapshotResponse snapshot,
        string code)
    {
        // Read one routine outcome from a composed snapshot.
        return snapshot.Routines.Single(routine => routine.Code == code).Outcome;
    }
}
