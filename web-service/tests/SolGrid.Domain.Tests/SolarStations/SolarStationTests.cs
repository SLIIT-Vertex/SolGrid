/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: SolarStationTests.cs
 * Description: Verifies microgrid entity invariants and lifecycle transitions.
 * Contributor: Kavishi Godage
 */

using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using SolGrid.Domain.ValueObjects;
using Xunit;

namespace SolGrid.Domain.Tests.SolarStations;

public sealed class SolarStationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_EmptySlots_HasNonNegativeDerivedCounts()
    {
        // Allow a node with no configured batteries without accepting a mutable count.
        var station = Create();
        Assert.Equal("ST-1", station.Code);
        Assert.Equal("Station", station.Name);
        Assert.True(station.IsActive);
        Assert.Equal(0, station.TotalSlotCount);
        Assert.Equal(0, station.AvailableSlotCount);
        Assert.False(station.CanAcceptBookingsAt(Now));
        Assert.Equal(Now, station.CreatedAt);
        Assert.Equal(Now, station.UpdatedAt);
    }

    [Theory]
    [InlineData("", "Station", 10)]
    [InlineData("ST-1", " ", 10)]
    [InlineData("ST-1", "Station", 0)]
    [InlineData("ST-1", "Station", -1)]
    public void InvalidDetails_AreRejectedWithoutPartialMutation(string code, string name, int capacity)
    {
        // A failed edit must preserve all previously valid details and audit metadata.
        var station = Create();
        Assert.ThrowsAny<ArgumentException>(() => station.UpdateDetails(
            code, name, "New address", GeoCoordinates.Create(0, 0), capacity, Now.AddMinutes(1)));
        Assert.Equal("ST-1", station.Code);
        Assert.Equal("Station", station.Name);
        Assert.Equal("Colombo", station.AddressLine);
        Assert.Equal(50m, station.CapacityKw);
        Assert.Equal(Now, station.UpdatedAt);
        Assert.ThrowsAny<ArgumentException>(() => SolarStation.Create("station-1", code, name,
            "Colombo", GeoCoordinates.Create(0, 0), capacity, [], Schedule(), Now));
    }

    [Fact]
    public void UpdateDetails_ValidEdit_UpdatesCapacityAndAudit()
    {
        // Preserve station identity while replacing normalized details and capacity.
        var station = Create();
        station.UpdateDetails(" st-2 ", " Updated ", " Kandy ", GeoCoordinates.Create(7, 80), 75m, Now.AddMinutes(1));
        Assert.Equal("station-1", station.Id);
        Assert.Equal("ST-2", station.Code);
        Assert.Equal("Updated", station.Name);
        Assert.Equal(75m, station.CapacityKw);
        Assert.Equal(Now, station.CreatedAt);
        Assert.Equal(Now.AddMinutes(1), station.UpdatedAt);
    }

    [Fact]
    public void ActivateAndDeactivate_ChangesBookability()
    {
        // Combine station state, weekly schedule, and slot availability for local eligibility.
        var station = Create(Slot());
        Assert.True(station.CanAcceptBookingsAt(Now));
        Assert.False(station.CanAcceptBookingsAt(Now.AddHours(1)));
        station.Deactivate(Now.AddMinutes(1));
        Assert.Equal(StationStatus.Inactive, station.Status);
        Assert.False(station.CanAcceptBookingsAt(Now));
        station.Activate(Now.AddMinutes(2));
        Assert.True(station.CanAcceptBookingsAt(Now));
        Assert.Equal(Now.AddMinutes(2), station.UpdatedAt);
    }

    [Theory]
    [InlineData(SlotStatus.Reserved)]
    [InlineData(SlotStatus.Occupied)]
    public void CommittedSlot_BlocksDeactivationAndRemoval(SlotStatus status)
    {
        // Guard locally known commitments without querying reservation persistence from Domain.
        var station = Create(Slot(status: status));
        Assert.Throws<InvalidOperationException>(() => station.Deactivate(Now.AddMinutes(1)));
        Assert.Throws<InvalidOperationException>(() => station.RemoveSlot("slot-1", Now.AddMinutes(1)));
        Assert.True(station.IsActive);
        Assert.Equal(Now, station.UpdatedAt);
        Assert.Single(station.Slots);
    }

    [Fact]
    public void SlotOwnershipAndUniqueness_AreEnforcedAtCreationAndAddition()
    {
        // Reject foreign slots and duplicate identifiers or numbers on both aggregate entry paths.
        var foreign = Slot(stationId: "station-2");
        Assert.Throws<ArgumentException>(() => Create(foreign));
        Assert.Throws<ArgumentException>(() => Create(Slot(), Slot()));
        var station = Create(Slot());
        Assert.Throws<ArgumentException>(() => station.AddSlot(foreign, Now));
        Assert.Throws<InvalidOperationException>(() => station.AddSlot(Slot(number: 2), Now));
        Assert.Throws<InvalidOperationException>(() => station.AddSlot(Slot(id: "slot-2"), Now));
        Assert.Throws<InvalidOperationException>(() => station.ChangeSlotStatus("missing", SlotStatus.OutOfService, Now));
        Assert.Single(station.Slots);
    }

    [Fact]
    public void AvailabilityCounts_FollowSlotStateAndRemoval()
    {
        // Derive counts from real slots across maintenance transitions and last-slot removal.
        var station = Create();
        station.AddSlot(Slot(), Now);
        Assert.Equal(1, station.AvailableSlotCount);
        station.ChangeSlotStatus("slot-1", SlotStatus.OutOfService, Now);
        Assert.Equal(0, station.AvailableSlotCount);
        station.ChangeSlotStatus("slot-1", SlotStatus.Available, Now);
        Assert.Equal(1, station.AvailableSlotCount);
        station.RemoveSlot("slot-1", Now);
        Assert.Equal(0, station.TotalSlotCount);
        Assert.Equal(0, station.AvailableSlotCount);
    }

    [Fact]
    public void Collections_CannotBeMutatedThroughPublicViews()
    {
        // Stop callers from bypassing ownership and overlap checks through collection casts.
        var station = Create(Slot());
        Assert.Throws<NotSupportedException>(() => ((IList<EnergyBookingSlot>)station.Slots).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<OperatingWindow>)station.Schedule).Clear());
    }

    [Fact]
    public void Schedule_RejectsOverlapAndNullEntriesWithoutChangingState()
    {
        // Keep the previous schedule when any replacement window is invalid.
        var station = Create();
        var overlap = OperatingWindow.Create(DayOfWeek.Wednesday, new TimeOnly(9, 0), new TimeOnly(11, 0));
        Assert.Throws<ArgumentException>(() => station.ReplaceSchedule([Schedule()[0], overlap], Now.AddMinutes(1)));
        Assert.Throws<ArgumentNullException>(() => station.ReplaceSchedule([Schedule()[0], null!], Now));
        Assert.Throws<ArgumentNullException>(() => Create(null!));
        Assert.Throws<ArgumentNullException>(() => Create(Slot(), null!));
        Assert.Single(station.Schedule);
        Assert.Equal(Now, station.UpdatedAt);
    }

    [Fact]
    public void Schedule_AllowsAdjacentWindowsAndExcludesClosingBoundary()
    {
        // Avoid double counting boundaries while allowing consecutive operating windows.
        var station = Create();
        station.ReplaceSchedule([Schedule()[0],
            OperatingWindow.Create(DayOfWeek.Wednesday, new TimeOnly(10, 0), new TimeOnly(12, 0))], Now);
        Assert.True(station.IsOpenAt(Now));
        Assert.True(station.IsOpenAt(Now.AddHours(2)));
        Assert.False(station.IsOpenAt(Now.AddHours(4)));
        Assert.False(station.IsOpenAt(Now.AddDays(1)));
    }

    [Fact]
    public void Restore_UndefinedStationStatus_Rejects()
    {
        // Reject corrupted numeric lifecycle values during persistence rehydration.
        Assert.Throws<ArgumentOutOfRangeException>(() => SolarStation.Restore("station-1", "ST-1", "Station",
            "Colombo", GeoCoordinates.Create(0, 0), 50m, [], Schedule(), (StationStatus)999, Now, Now));
    }

    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    public void Coordinates_AcceptsInclusiveBoundaries(double latitude, double longitude)
    {
        // Verify the assignment GPS ranges include valid boundary positions.
        var location = GeoCoordinates.Create(latitude, longitude);
        Assert.Equal(latitude, location.Latitude);
        Assert.Equal(longitude, location.Longitude);
    }

    [Theory]
    [InlineData(-91, 0)]
    [InlineData(91, 0)]
    [InlineData(0, -181)]
    [InlineData(0, 181)]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.PositiveInfinity)]
    public void Coordinates_RejectsInvalidValues(double latitude, double longitude)
    {
        // Reject out-of-range and non-finite GPS coordinates in Domain as well as requests.
        Assert.Throws<ArgumentOutOfRangeException>(() => GeoCoordinates.Create(latitude, longitude));
    }

    [Fact]
    public void OperatingWindow_RejectsInvalidDayAndNonIncreasingTimes()
    {
        // Enforce same-day operating intervals with a supported weekday.
        Assert.Throws<ArgumentOutOfRangeException>(() => OperatingWindow.Create((DayOfWeek)9, new(8, 0), new(10, 0)));
        Assert.Throws<ArgumentException>(() => OperatingWindow.Create(DayOfWeek.Monday, new(8, 0), new(8, 0)));
        Assert.Throws<ArgumentException>(() => OperatingWindow.Create(DayOfWeek.Monday, new(10, 0), new(8, 0)));
    }

    private static SolarStation Create(params EnergyBookingSlot[] slots)
    {
        // Build a station open on the fixed test date with only the requested slots.
        return SolarStation.Create("station-1", " st-1 ", " Station ", "Colombo",
            GeoCoordinates.Create(6.9, 79.8), 50m, slots, Schedule(), Now);
    }

    private static OperatingWindow[] Schedule()
    {
        // Provide a deterministic weekly operating window around the test instant.
        return [OperatingWindow.Create(DayOfWeek.Wednesday, new TimeOnly(8, 0), new TimeOnly(10, 0))];
    }

    private static EnergyBookingSlot Slot(string id = "slot-1", string stationId = "station-1",
        int number = 1, SlotStatus status = SlotStatus.Available)
    {
        // Build an owned or foreign slot in the requested test state.
        return EnergyBookingSlot.Restore(id, stationId, number, 10m, Now, Now.AddHours(1), status, Now, Now);
    }
}
