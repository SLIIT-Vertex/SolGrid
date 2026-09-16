/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: SolarStationDocument.cs
 * Description: Defines the MongoDB document shape for SolarStationInfo persistence.
 * Contributor: Kavishi Godage
 */

using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using SolGrid.Domain.ValueObjects;

namespace SolGrid.Infrastructure.Persistence.MongoDb.Documents;

internal sealed class SolarStationDocument
{
    [BsonId]
    public string Id { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string AddressLine { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public GeoJsonPoint<GeoJson2DGeographicCoordinates> GeoLocation { get; init; } =
        GeoJson.Point(new GeoJson2DGeographicCoordinates(0d, 0d));

    public decimal CapacityKw { get; init; }

    public StationStatus Status { get; init; }

    public int TotalSlotCount { get; init; }

    public int AvailableSlotCount { get; init; }

    public IReadOnlyList<OwnedEnergyBookingSlotDocument> Slots { get; init; } = [];

    public IReadOnlyList<OperatingWindowDocument> Schedule { get; init; } = [];

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public static SolarStationDocument FromDomain(SolarStation station)
    {
        // Convert a domain station into the MongoDB SolarStationInfo document shape.
        return new SolarStationDocument
        {
            Id = station.Id,
            Code = station.Code,
            Name = station.Name,
            AddressLine = station.AddressLine,
            Latitude = station.Location.Latitude,
            Longitude = station.Location.Longitude,
            GeoLocation = GeoJson.Point(
                new GeoJson2DGeographicCoordinates(station.Location.Longitude, station.Location.Latitude)),
            CapacityKw = station.CapacityKw,
            Status = station.Status,
            TotalSlotCount = station.TotalSlotCount,
            AvailableSlotCount = station.AvailableSlotCount,
            Slots = station.Slots.Select(OwnedEnergyBookingSlotDocument.FromDomain).ToArray(),
            Schedule = station.Schedule.Select(OperatingWindowDocument.FromDomain).ToArray(),
            CreatedAtUtc = station.CreatedAt.UtcDateTime,
            UpdatedAtUtc = station.UpdatedAt.UtcDateTime
        };
    }

    public SolarStation ToDomain()
    {
        // Restore a domain station from the MongoDB SolarStationInfo document shape.
        return SolarStation.Restore(
            Id,
            Code,
            Name,
            AddressLine,
            GeoCoordinates.Create(Latitude, Longitude),
            CapacityKw,
            Slots.Select(slot => slot.ToDomain()),
            Schedule.Select(window => window.ToDomain()),
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

internal sealed class OwnedEnergyBookingSlotDocument
{
    public string Id { get; init; } = string.Empty;

    public string StationId { get; init; } = string.Empty;

    public int SlotNumber { get; init; }

    public decimal BatteryCapacityKwh { get; init; }

    public DateTime StartTimeUtc { get; init; }

    public DateTime EndTimeUtc { get; init; }

    public SlotStatus Status { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public static OwnedEnergyBookingSlotDocument FromDomain(EnergyBookingSlot slot)
    {
        // Convert an owned booking slot into the nested SolarStationInfo document shape.
        return new OwnedEnergyBookingSlotDocument
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
        // Restore an owned booking slot from nested SolarStationInfo persistence.
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

internal sealed class OperatingWindowDocument
{
    public DayOfWeek Day { get; init; }

    public string OpensAt { get; init; } = string.Empty;

    public string ClosesAt { get; init; } = string.Empty;

    public static OperatingWindowDocument FromDomain(OperatingWindow window)
    {
        // Persist weekday windows as round-trippable clock strings.
        return new OperatingWindowDocument
        {
            Day = window.Day,
            OpensAt = window.OpensAt.ToString("HH:mm:ss"),
            ClosesAt = window.ClosesAt.ToString("HH:mm:ss")
        };
    }

    public OperatingWindow ToDomain()
    {
        // Restore a weekday availability window from persisted clock strings.
        return OperatingWindow.Create(Day, TimeOnly.Parse(OpensAt), TimeOnly.Parse(ClosesAt));
    }
}
