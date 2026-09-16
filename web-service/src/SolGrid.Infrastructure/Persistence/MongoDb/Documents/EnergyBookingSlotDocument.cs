/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: EnergyBookingSlotDocument.cs
 * Description: Defines the MongoDB document shape for energy booking slot persistence.
 * Contributor: Kavishi Godage
 */

using MongoDB.Bson.Serialization.Attributes;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;

namespace SolGrid.Infrastructure.Persistence.MongoDb.Documents;

internal sealed class EnergyBookingSlotDocument
{
    [BsonId]
    public string Id { get; init; } = string.Empty;

    public string StationId { get; init; } = string.Empty;

    public int SlotNumber { get; init; }

    public decimal BatteryCapacityKwh { get; init; }

    public DateTime StartTimeUtc { get; init; }

    public DateTime EndTimeUtc { get; init; }

    public SlotStatus Status { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public static EnergyBookingSlotDocument FromDomain(EnergyBookingSlot slot)
    {
        // Convert a domain booking slot into the MongoDB EnergyBookingSlots document shape.
        return new EnergyBookingSlotDocument
        {
            Id = slot.Id,
            StationId = slot.StationId,
            SlotNumber = slot.SlotNumber,
            BatteryCapacityKwh = slot.BatteryCapacityKwh,
            StartTimeUtc = slot.StartTime.UtcDateTime,
            EndTimeUtc = slot.EndTime.UtcDateTime,
            Status = slot.Status,
            CreatedAtUtc = slot.CreatedAt.UtcDateTime,
            UpdatedAtUtc = slot.UpdatedAt.UtcDateTime
        };
    }

    public EnergyBookingSlot ToDomain()
    {
        // Restore a domain booking slot without exposing this persistence type.
        return EnergyBookingSlot.Restore(
            Id,
            StationId,
            SlotNumber,
            BatteryCapacityKwh,
            ToUtcOffset(StartTimeUtc),
            ToUtcOffset(EndTimeUtc),
            Status,
            ToUtcOffset(CreatedAtUtc),
            ToUtcOffset(UpdatedAtUtc));
    }

    private static DateTimeOffset ToUtcOffset(DateTime value)
    {
        // Convert persisted timestamps to explicit UTC offsets.
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
