/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: EnergyBookingSlot.cs
 * Description: Represents one bookable battery storage slot inside a solar station.
 * Contributor: Kavishi Godage
 */

using SolGrid.Domain.Enums;

namespace SolGrid.Domain.Entities;

public sealed class EnergyBookingSlot
{
    private EnergyBookingSlot(
        string id,
        int slotNumber,
        decimal batteryCapacityKwh,
        SlotStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        // Initialize a validated slot instance for creation or persistence rehydration.
        Id = RequireValue(id, nameof(id));
        SlotNumber = RequirePositive(slotNumber, nameof(slotNumber));
        BatteryCapacityKwh = RequirePositive(batteryCapacityKwh, nameof(batteryCapacityKwh));
        Status = RequireDefinedEnum(status, nameof(status));
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public string Id { get; }

    public int SlotNumber { get; }

    public decimal BatteryCapacityKwh { get; private set; }

    public SlotStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsAvailable => Status == SlotStatus.Available;

    public bool IsCommitted => Status is SlotStatus.Reserved or SlotStatus.Occupied;

    public static EnergyBookingSlot Create(
        string id,
        int slotNumber,
        decimal batteryCapacityKwh,
        DateTimeOffset createdAt)
    {
        // Create a new slot that is immediately open for energy bookings.
        return new EnergyBookingSlot(
            id,
            slotNumber,
            batteryCapacityKwh,
            SlotStatus.Available,
            createdAt,
            createdAt);
    }

    public static EnergyBookingSlot Restore(
        string id,
        int slotNumber,
        decimal batteryCapacityKwh,
        SlotStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        // Rehydrate a slot from persistence without exposing persistence-specific types.
        return new EnergyBookingSlot(id, slotNumber, batteryCapacityKwh, status, createdAt, updatedAt);
    }

    public void ChangeBatteryCapacity(decimal batteryCapacityKwh, DateTimeOffset updatedAt)
    {
        // Adjust the installed battery capacity after hardware changes.
        BatteryCapacityKwh = RequirePositive(batteryCapacityKwh, nameof(batteryCapacityKwh));
        MarkUpdated(updatedAt);
    }

    public void Reserve(DateTimeOffset updatedAt)
    {
        // Hold the slot for a confirmed energy reservation.
        RequireCurrentStatus(SlotStatus.Available, "reserved");
        Status = SlotStatus.Reserved;
        MarkUpdated(updatedAt);
    }

    public void MarkOccupied(DateTimeOffset updatedAt)
    {
        // Record that the prosumer has arrived and the slot is physically in use.
        RequireCurrentStatus(SlotStatus.Reserved, "marked occupied");
        Status = SlotStatus.Occupied;
        MarkUpdated(updatedAt);
    }

    public void Release(DateTimeOffset updatedAt)
    {
        // Return a reserved or occupied slot to the bookable pool.
        if (!IsCommitted)
        {
            throw new InvalidOperationException(
                $"A slot in '{Status}' state cannot be released.");
        }

        Status = SlotStatus.Available;
        MarkUpdated(updatedAt);
    }

    public void TakeOutOfService(DateTimeOffset updatedAt)
    {
        // Withdraw a slot from bookings only when nothing is committed against it.
        if (IsCommitted)
        {
            throw new InvalidOperationException(
                "A slot with a reserved or occupied booking cannot be taken out of service.");
        }

        Status = SlotStatus.OutOfService;
        MarkUpdated(updatedAt);
    }

    public void ReturnToService(DateTimeOffset updatedAt)
    {
        // Bring a repaired slot back into the bookable pool.
        RequireCurrentStatus(SlotStatus.OutOfService, "returned to service");
        Status = SlotStatus.Available;
        MarkUpdated(updatedAt);
    }

    public void ApplyStatus(SlotStatus status, DateTimeOffset updatedAt)
    {
        // Route an operator requested status onto the transition that enforces its rules.
        switch (RequireDefinedEnum(status, nameof(status)))
        {
            case SlotStatus.Available:
                if (Status == SlotStatus.OutOfService)
                {
                    ReturnToService(updatedAt);
                    return;
                }

                Release(updatedAt);
                return;

            case SlotStatus.Reserved:
                Reserve(updatedAt);
                return;

            case SlotStatus.Occupied:
                MarkOccupied(updatedAt);
                return;

            case SlotStatus.OutOfService:
                TakeOutOfService(updatedAt);
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Slot status is not supported.");
        }
    }

    private void RequireCurrentStatus(SlotStatus expected, string action)
    {
        // Reject transitions that do not start from the only valid source state.
        if (Status != expected)
        {
            throw new InvalidOperationException(
                $"A slot in '{Status}' state cannot be {action}.");
        }
    }

    private static string RequireValue(string value, string parameterName)
    {
        // Reject missing text values before they enter the domain model.
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    private static int RequirePositive(int value, string parameterName)
    {
        // Reject non-positive slot numbering that clients could never address.
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
        }

        return value;
    }

    private static decimal RequirePositive(decimal value, string parameterName)
    {
        // Reject non-positive capacity values that cannot describe real hardware.
        if (value <= 0m)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
        }

        return value;
    }

    private static TEnum RequireDefinedEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        // Reject undefined enum values before they become domain state.
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value is not supported.");
        }

        return value;
    }

    private void MarkUpdated(DateTimeOffset updatedAt)
    {
        // Capture the latest domain mutation timestamp.
        UpdatedAt = updatedAt;
    }
}
