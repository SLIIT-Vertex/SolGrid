/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: SolarStationResponseMapper.cs
 * Description: Maps solar station domain state onto API response contracts.
 * Contributor: Kavishi Godage
 */

using SolGrid.Domain.Entities;
using SolGrid.Domain.ValueObjects;

namespace SolGrid.Application.SolarStations.Responses;

public static class SolarStationResponseMapper
{
    public static SolarStationResponse ToResponse(SolarStation station, GeoCoordinates? origin = null)
    {
        // Project a station, optionally including its distance from a prosumer position.
        ArgumentNullException.ThrowIfNull(station);

        return new SolarStationResponse
        {
            Id = station.Id,
            Code = station.Code,
            Name = station.Name,
            AddressLine = station.AddressLine,
            Location = ToResponse(station.Location),
            CapacityKw = station.CapacityKw,
            Status = station.Status,
            TotalSlotCount = station.TotalSlotCount,
            AvailableSlotCount = station.AvailableSlotCount,
            Slots = station.Slots.Select(ToResponse).ToArray(),
            Schedule = station.Schedule.Select(ToResponse).ToArray(),
            DistanceKilometers = origin is null ? null : station.DistanceInKilometersFrom(origin),
            CreatedAt = station.CreatedAt,
            UpdatedAt = station.UpdatedAt
        };
    }

    public static EnergyBookingSlotResponse ToResponse(EnergyBookingSlot slot)
    {
        // Project one battery storage slot onto its response contract.
        ArgumentNullException.ThrowIfNull(slot);

        return new EnergyBookingSlotResponse
        {
            Id = slot.Id,
            SlotNumber = slot.SlotNumber,
            BatteryCapacityKwh = slot.BatteryCapacityKwh,
            Status = slot.Status,
            CreatedAt = slot.CreatedAt,
            UpdatedAt = slot.UpdatedAt
        };
    }

    public static OperatingWindowResponse ToResponse(OperatingWindow window)
    {
        // Project one weekday availability window onto its response contract.
        ArgumentNullException.ThrowIfNull(window);

        return new OperatingWindowResponse
        {
            Day = window.Day,
            OpensAt = window.OpensAt,
            ClosesAt = window.ClosesAt
        };
    }

    public static GeoCoordinatesResponse ToResponse(GeoCoordinates location)
    {
        // Project a GPS position onto its response contract.
        ArgumentNullException.ThrowIfNull(location);

        return new GeoCoordinatesResponse
        {
            Latitude = location.Latitude,
            Longitude = location.Longitude
        };
    }
}
