/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: SolarStation.cs
 * Description: Represents a microgrid solar station hub with slots and a weekly schedule.
 * Contributor: Kavishi Godage
 */

using SolGrid.Domain.Enums;
using SolGrid.Domain.ValueObjects;

namespace SolGrid.Domain.Entities;

public sealed class SolarStation
{
    private readonly List<EnergyBookingSlot> slots;
    private readonly List<OperatingWindow> schedule;

    private SolarStation(
        string id,
        string code,
        string name,
        string addressLine,
        GeoCoordinates location,
        decimal capacityKw,
        IEnumerable<EnergyBookingSlot> slots,
        IEnumerable<OperatingWindow> schedule,
        StationStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        // Initialize a validated station instance for creation or persistence rehydration.
        Id = RequireValue(id, nameof(id));
        Code = NormalizeCode(code);
        Name = RequireValue(name, nameof(name));
        AddressLine = RequireValue(addressLine, nameof(addressLine));
        Location = RequireReference(location, nameof(location));
        CapacityKw = RequirePositive(capacityKw, nameof(capacityKw));
        this.slots = BuildSlots(slots);
        this.schedule = BuildSchedule(schedule);
        Status = RequireDefinedEnum(status, nameof(status));
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public string Id { get; }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public string AddressLine { get; private set; }

    public GeoCoordinates Location { get; private set; }

    public decimal CapacityKw { get; private set; }

    public StationStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<EnergyBookingSlot> Slots => slots.AsReadOnly();

    public IReadOnlyList<OperatingWindow> Schedule => schedule.AsReadOnly();

    public bool IsActive => Status == StationStatus.Active;

    public int TotalSlotCount => slots.Count;

    public int AvailableSlotCount => slots.Count(slot => slot.IsAvailable);

    public bool HasCommittedSlots => slots.Any(slot => slot.IsCommitted);

    public static SolarStation Create(
        string id,
        string code,
        string name,
        string addressLine,
        GeoCoordinates location,
        decimal capacityKw,
        IEnumerable<EnergyBookingSlot> slots,
        IEnumerable<OperatingWindow> schedule,
        DateTimeOffset createdAt)
    {
        // Create a new active station that is immediately open for energy bookings.
        return new SolarStation(
            id,
            code,
            name,
            addressLine,
            location,
            capacityKw,
            slots,
            schedule,
            StationStatus.Active,
            createdAt,
            createdAt);
    }

    public static SolarStation Restore(
        string id,
        string code,
        string name,
        string addressLine,
        GeoCoordinates location,
        decimal capacityKw,
        IEnumerable<EnergyBookingSlot> slots,
        IEnumerable<OperatingWindow> schedule,
        StationStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        // Rehydrate a station from persistence without exposing persistence-specific types.
        return new SolarStation(
            id,
            code,
            name,
            addressLine,
            location,
            capacityKw,
            slots,
            schedule,
            status,
            createdAt,
            updatedAt);
    }

    public void UpdateDetails(
        string code,
        string name,
        string addressLine,
        GeoCoordinates location,
        decimal capacityKw,
        DateTimeOffset updatedAt)
    {
        // Update editable station details while preserving invariant checks.
        var normalizedCode = NormalizeCode(code);
        var normalizedName = RequireValue(name, nameof(name));
        var normalizedAddress = RequireValue(addressLine, nameof(addressLine));
        var validatedLocation = RequireReference(location, nameof(location));
        var validatedCapacity = RequirePositive(capacityKw, nameof(capacityKw));
        Code = normalizedCode;
        Name = normalizedName;
        AddressLine = normalizedAddress;
        Location = validatedLocation;
        CapacityKw = validatedCapacity;
        MarkUpdated(updatedAt);
    }

    public void ReplaceSchedule(IEnumerable<OperatingWindow> schedule, DateTimeOffset updatedAt)
    {
        // Swap the weekly availability schedule as one validated set.
        var replacement = BuildSchedule(schedule);
        this.schedule.Clear();
        this.schedule.AddRange(replacement);
        MarkUpdated(updatedAt);
    }

    public void AddSlot(EnergyBookingSlot slot, DateTimeOffset updatedAt)
    {
        // Register additional battery storage hardware against this station.
        RequireReference(slot, nameof(slot));
        RequireOwnedSlot(slot);

        if (slots.Any(existing => existing.Id == slot.Id))
        {
            throw new InvalidOperationException($"Slot '{slot.Id}' already belongs to this station.");
        }

        if (slots.Any(existing => existing.SlotNumber == slot.SlotNumber))
        {
            throw new InvalidOperationException($"Slot number {slot.SlotNumber} is already used by this station.");
        }

        slots.Add(slot);
        SortSlots();
        MarkUpdated(updatedAt);
    }

    public void RemoveSlot(string slotId, DateTimeOffset updatedAt)
    {
        // Retire uncommitted hardware while keeping the derived slot count non-negative.
        var slot = RequireSlot(slotId);

        if (slot.IsCommitted)
        {
            throw new InvalidOperationException(
                "A slot with a reserved or occupied booking cannot be removed.");
        }

        slots.Remove(slot);
        MarkUpdated(updatedAt);
    }

    public EnergyBookingSlot ChangeSlotStatus(string slotId, SlotStatus status, DateTimeOffset updatedAt)
    {
        // Apply a slot state change through the slot's own transition rules.
        var slot = RequireSlot(slotId);
        slot.ApplyStatus(status, updatedAt);
        MarkUpdated(updatedAt);

        return slot;
    }

    public EnergyBookingSlot? FindSlot(string slotId)
    {
        // Look up one slot without forcing callers to scan the collection.
        return string.IsNullOrWhiteSpace(slotId)
            ? null
            : slots.SingleOrDefault(slot => slot.Id == slotId.Trim());
    }

    public bool IsOpenAt(DateTimeOffset instant)
    {
        // Check the weekly schedule for the weekday and time of the supplied instant.
        var time = TimeOnly.FromTimeSpan(instant.TimeOfDay);

        return schedule.Any(window => window.Day == instant.DayOfWeek && window.Covers(time));
    }

    public bool CanAcceptBookingsAt(DateTimeOffset instant)
    {
        // Combine station status, schedule, and slot availability into one bookability check.
        return IsActive && IsOpenAt(instant) && slots.Any(slot => slot.IsAvailableAt(instant));
    }

    public double DistanceInKilometersFrom(GeoCoordinates origin)
    {
        // Report how far this station is from a prosumer position.
        RequireReference(origin, nameof(origin));

        return origin.DistanceInKilometersTo(Location);
    }

    public void Activate(DateTimeOffset updatedAt)
    {
        // Mark the station as available for new energy reservations.
        Status = StationStatus.Active;
        MarkUpdated(updatedAt);
    }

    public void Deactivate(DateTimeOffset updatedAt)
    {
        // Withdraw the station from new bookings. Callers must first confirm that no active
        // reservation exists, because that check spans the reservation module.
        if (HasCommittedSlots)
        {
            throw new InvalidOperationException(
                "A station with reserved or occupied slots cannot be deactivated.");
        }

        Status = StationStatus.Inactive;
        MarkUpdated(updatedAt);
    }

    private EnergyBookingSlot RequireSlot(string slotId)
    {
        // Resolve a slot or report that the station does not own it.
        return FindSlot(slotId)
            ?? throw new InvalidOperationException($"Slot '{slotId}' does not belong to this station.");
    }

    private void SortSlots()
    {
        // Keep slots in a stable, client friendly order.
        slots.Sort((left, right) => left.SlotNumber.CompareTo(right.SlotNumber));
    }

    private List<EnergyBookingSlot> BuildSlots(IEnumerable<EnergyBookingSlot> slots)
    {
        // Validate every slot and its ownership before exposing the station aggregate.
        var slotList = RequireReference(slots, nameof(slots))
            .ToList();

        foreach (var slot in slotList)
        {
            RequireReference(slot, nameof(slots));
            RequireOwnedSlot(slot);
        }

        slotList.Sort((left, right) => left.SlotNumber.CompareTo(right.SlotNumber));

        if (slotList.Select(slot => slot.SlotNumber).Distinct().Count() != slotList.Count)
        {
            throw new ArgumentException("Battery storage slot numbers must be unique per station.", nameof(slots));
        }

        if (slotList.Select(slot => slot.Id).Distinct(StringComparer.Ordinal).Count() != slotList.Count)
        {
            throw new ArgumentException("Battery storage slot ids must be unique per station.", nameof(slots));
        }

        return slotList;
    }

    private void RequireOwnedSlot(EnergyBookingSlot slot)
    {
        // Reject slot associations that contradict the slot's immutable station identifier.
        if (slot.StationId != Id)
        {
            throw new ArgumentException("The booking slot must belong to this station.", nameof(slot));
        }
    }

    private static List<OperatingWindow> BuildSchedule(IEnumerable<OperatingWindow> schedule)
    {
        // Validate that the weekly schedule is present and free of overlapping windows.
        var windows = RequireReference(schedule, nameof(schedule))
            .Select(window => RequireReference(window, nameof(schedule)))
            .OrderBy(window => window.Day)
            .ThenBy(window => window.OpensAt)
            .ToList();

        if (windows.Count == 0)
        {
            throw new ArgumentException("A station requires at least one operating window.", nameof(schedule));
        }

        for (var index = 1; index < windows.Count; index++)
        {
            if (windows[index].Overlaps(windows[index - 1]))
            {
                throw new ArgumentException(
                    "Operating windows for the same day must not overlap.",
                    nameof(schedule));
            }
        }

        return windows;
    }

    private static string NormalizeCode(string code)
    {
        // Normalize the station code for consistent uniqueness checks and lookups.
        return RequireValue(code, nameof(code)).ToUpperInvariant();
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

    private static decimal RequirePositive(decimal value, string parameterName)
    {
        // Reject non-positive capacity values that cannot describe a real grid hub.
        if (value <= 0m)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
        }

        return value;
    }

    private static T RequireReference<T>(T value, string parameterName)
        where T : class
    {
        // Reject missing reference values before they become domain state.
        return value ?? throw new ArgumentNullException(parameterName);
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
