/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: EnergyBookingSlotTests.cs
 * Description: Verifies microgrid entity invariants and lifecycle transitions.
 * Contributor: Kavishi Godage
 */

using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using SolGrid.Domain.ValueObjects;
using Xunit;

namespace SolGrid.Domain.Tests.SolarStations;

public sealed class EnergyBookingSlotTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_NormalizesOwnershipAndTimesAndStartsAvailable()
    {
        // Verify identifiers, UTC booking times, state, and audit values are authoritative.
        var slot = EnergyBookingSlot.Create(" slot-1 ", " station-1 ", 1, 10m,
            Now.ToOffset(TimeSpan.FromHours(5.5)), Now.AddHours(1), Now);

        Assert.Equal("slot-1", slot.Id);
        Assert.Equal("station-1", slot.StationId);
        Assert.Equal(Now, slot.StartTime);
        Assert.Equal(TimeSpan.Zero, slot.StartTime.Offset);
        Assert.Equal(Now.AddHours(1), slot.EndTime);
        Assert.Equal(10m, slot.BatteryCapacityKwh);
        Assert.True(slot.IsActive);
        Assert.True(slot.IsAvailable);
        Assert.False(slot.IsCommitted);
        Assert.Equal(Now, slot.CreatedAt);
        Assert.Equal(Now, slot.UpdatedAt);
    }

    [Theory]
    [InlineData("", "station-1", 1, 10)]
    [InlineData("slot-1", " ", 1, 10)]
    [InlineData("slot-1", "station-1", 0, 10)]
    [InlineData("slot-1", "station-1", 1, 0)]
    [InlineData("slot-1", "station-1", 1, -1)]
    public void Create_InvalidIdentityOrCapacity_Rejects(string id, string stationId, int number, int capacity)
    {
        // Reject malformed slot identity and non-positive hardware specifications.
        Assert.ThrowsAny<ArgumentException>(() => EnergyBookingSlot.Create(
            id, stationId, number, capacity, Now, Now.AddHours(1), Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_NonIncreasingTimeRange_Rejects(int hours)
    {
        // Compare instants even when the caller sends different offsets.
        Assert.Throws<ArgumentException>(() => EnergyBookingSlot.Create(
            "slot-1", "station-1", 1, 10m, Now.ToOffset(TimeSpan.FromHours(-4)),
            Now.AddHours(hours).ToOffset(TimeSpan.FromHours(5.5)), Now));
    }

    [Theory]
    [InlineData(SlotStatus.Available, SlotStatus.Reserved)]
    [InlineData(SlotStatus.Available, SlotStatus.OutOfService)]
    [InlineData(SlotStatus.Reserved, SlotStatus.Occupied)]
    [InlineData(SlotStatus.Reserved, SlotStatus.Available)]
    [InlineData(SlotStatus.Occupied, SlotStatus.Available)]
    [InlineData(SlotStatus.OutOfService, SlotStatus.Available)]
    public void ApplyStatus_LegalTransition_UpdatesStateAndAudit(SlotStatus current, SlotStatus target)
    {
        // Verify every existing lifecycle edge and its availability semantics.
        var slot = Restore(current);
        slot.ApplyStatus(target, Now.AddMinutes(1));

        Assert.Equal(target, slot.Status);
        Assert.Equal(target == SlotStatus.Available, slot.IsAvailable);
        Assert.Equal(target != SlotStatus.OutOfService, slot.IsActive);
        Assert.Equal(target is SlotStatus.Reserved or SlotStatus.Occupied, slot.IsCommitted);
        Assert.Equal(Now, slot.CreatedAt);
        Assert.Equal(Now.AddMinutes(1), slot.UpdatedAt);
    }

    [Theory]
    [InlineData(SlotStatus.Available, SlotStatus.Occupied)]
    [InlineData(SlotStatus.Reserved, SlotStatus.OutOfService)]
    [InlineData(SlotStatus.Occupied, SlotStatus.OutOfService)]
    [InlineData(SlotStatus.Occupied, SlotStatus.Reserved)]
    [InlineData(SlotStatus.OutOfService, SlotStatus.Reserved)]
    [InlineData(SlotStatus.OutOfService, SlotStatus.Occupied)]
    public void ApplyStatus_IllegalTransition_PreservesState(SlotStatus current, SlotStatus target)
    {
        // Ensure a rejected transition cannot change availability or the audit timestamp.
        var slot = Restore(current);
        Assert.Throws<InvalidOperationException>(() => slot.ApplyStatus(target, Now.AddMinutes(1)));
        Assert.Equal(current, slot.Status);
        Assert.Equal(Now, slot.UpdatedAt);
    }

    [Fact]
    public void UndefinedStatus_IsRejectedByRestoreAndMutation()
    {
        // Prevent numeric enum values outside the defined lifecycle from entering domain state.
        Assert.Throws<ArgumentOutOfRangeException>(() => Restore((SlotStatus)999));
        var slot = Restore(SlotStatus.Available);
        Assert.Throws<ArgumentOutOfRangeException>(() => slot.ApplyStatus((SlotStatus)999, Now));
        Assert.True(slot.IsAvailable);
    }

    [Fact]
    public void UpdateDetails_ValidReplacement_PreservesIdentity()
    {
        // Change capacity and schedule together without changing ownership or creation metadata.
        var slot = Restore(SlotStatus.Available);
        slot.UpdateDetails(20m, Now.AddHours(2), Now.AddHours(3), Now.AddMinutes(1));
        Assert.Equal("station-1", slot.StationId);
        Assert.Equal("slot-1", slot.Id);
        Assert.Equal(20m, slot.BatteryCapacityKwh);
        Assert.Equal(Now.AddHours(2), slot.StartTime);
        Assert.Equal(Now.AddHours(3), slot.EndTime);
        Assert.Equal(Now, slot.CreatedAt);
        Assert.Equal(Now.AddMinutes(1), slot.UpdatedAt);
    }

    [Fact]
    public void UpdateDetails_InvalidRange_DoesNotPartiallyChangeCapacity()
    {
        // Verify validation failure leaves the entire previous configuration intact.
        var slot = Restore(SlotStatus.Available);
        Assert.Throws<ArgumentException>(() => slot.UpdateDetails(20m, Now, Now, Now.AddMinutes(1)));
        Assert.Equal(10m, slot.BatteryCapacityKwh);
        Assert.Equal(Now.AddHours(1), slot.EndTime);
        Assert.Equal(Now, slot.UpdatedAt);
    }

    [Theory]
    [InlineData(SlotStatus.Reserved)]
    [InlineData(SlotStatus.Occupied)]
    public void Reconfigure_CommittedSlot_Rejects(SlotStatus status)
    {
        // Prevent local capacity and schedule changes from invalidating a committed slot.
        var slot = Restore(status);
        Assert.Throws<InvalidOperationException>(() => slot.UpdateDetails(20m, Now, Now.AddHours(2), Now));
        Assert.Throws<InvalidOperationException>(() => slot.ChangeBatteryCapacity(20m, Now));
    }

    [Fact]
    public void IsAvailableAt_UsesHalfOpenIntervalAndStatus()
    {
        // Include the start, exclude the end, and compare availability at absolute instants.
        var slot = Restore(SlotStatus.Available);
        Assert.False(slot.IsAvailableAt(Now.AddTicks(-1)));
        Assert.True(slot.IsAvailableAt(Now.ToOffset(TimeSpan.FromHours(5.5))));
        Assert.True(slot.IsAvailableAt(Now.AddMinutes(30)));
        Assert.False(slot.IsAvailableAt(Now.AddHours(1)));
        slot.TakeOutOfService(Now);
        Assert.False(slot.IsAvailableAt(Now));
    }

    private static EnergyBookingSlot Restore(SlotStatus status)
    {
        // Build a deterministic slot in the lifecycle state under test.
        return EnergyBookingSlot.Restore("slot-1", "station-1", 1, 10m,
            Now, Now.AddHours(1), status, Now, Now);
    }
}
