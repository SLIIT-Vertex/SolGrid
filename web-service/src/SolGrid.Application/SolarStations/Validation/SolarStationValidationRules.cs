/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: SolarStationValidationRules.cs
 * Description: Provides shared validation helpers for solar station management requests.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.SolarStations.Interfaces;
using SolGrid.Application.SolarStations.Requests;
using SolGrid.Domain.ValueObjects;

namespace SolGrid.Application.SolarStations.Validation;

public static class SolarStationValidationRules
{
    public const int MaximumCodeLength = 32;
    public const int MaximumNameLength = 120;
    public const int MaximumAddressLineLength = 250;
    public const int MaximumSlotsPerStation = 100;
    public const double MinimumRadiusKilometers = 0.1d;
    public const double MaximumRadiusKilometers = 200d;
    public const int MaximumNearbyResults = 100;

    public static bool HasValue(string value)
    {
        // Check whether a required text value has meaningful content.
        return !string.IsNullOrWhiteSpace(value);
    }

    public static bool IsWithinLength(string value, int maximumLength)
    {
        // Check that a text value fits the persisted field length.
        return string.IsNullOrWhiteSpace(value) || value.Trim().Length <= maximumLength;
    }

    public static IEnumerable<string> ValidateIdentity(string code, string name, string addressLine)
    {
        // Validate the descriptive fields shared by station create and update requests.
        if (!HasValue(code))
        {
            yield return "Station code is required.";
        }
        else if (!IsWithinLength(code, MaximumCodeLength))
        {
            yield return $"Station code must not exceed {MaximumCodeLength} characters.";
        }

        if (!HasValue(name))
        {
            yield return "Station name is required.";
        }
        else if (!IsWithinLength(name, MaximumNameLength))
        {
            yield return $"Station name must not exceed {MaximumNameLength} characters.";
        }

        if (!HasValue(addressLine))
        {
            yield return "Station address is required.";
        }
        else if (!IsWithinLength(addressLine, MaximumAddressLineLength))
        {
            yield return $"Station address must not exceed {MaximumAddressLineLength} characters.";
        }
    }

    public static IEnumerable<string> ValidateLocation(GeoCoordinatesRequest? location)
    {
        // Validate that the supplied GPS position can describe a real place on earth.
        if (location is null)
        {
            yield return "Station GPS location is required.";
            yield break;
        }

        if (!GeoCoordinates.IsValidLatitude(location.Latitude))
        {
            yield return "Latitude must be between -90 and 90 degrees.";
        }

        if (!GeoCoordinates.IsValidLongitude(location.Longitude))
        {
            yield return "Longitude must be between -180 and 180 degrees.";
        }
    }

    public static IEnumerable<string> ValidateCapacity(decimal capacityKw)
    {
        // Validate the declared generation capacity of the grid hub.
        if (capacityKw <= 0m)
        {
            yield return "Station capacity in kW must be greater than zero.";
        }
    }

    public static IEnumerable<string> ValidateSlots(IReadOnlyList<EnergyBookingSlotRequest>? slots)
    {
        // Validate the full set of battery storage slots configured for a station.
        if (slots is null || slots.Count == 0)
        {
            yield return "At least one battery storage slot is required.";
            yield break;
        }

        if (slots.Count > MaximumSlotsPerStation)
        {
            yield return $"A station must not declare more than {MaximumSlotsPerStation} battery storage slots.";
        }

        foreach (var error in slots.SelectMany(ValidateSlot).Distinct(StringComparer.Ordinal))
        {
            yield return error;
        }

        var slotNumbers = slots
            .Where(slot => slot is not null)
            .Select(slot => slot.SlotNumber)
            .ToArray();

        if (slotNumbers.Distinct().Count() != slotNumbers.Length)
        {
            yield return "Battery storage slot numbers must be unique within a station.";
        }
    }

    public static IEnumerable<string> ValidateSlot(EnergyBookingSlotRequest? slot)
    {
        // Validate one battery storage slot definition.
        if (slot is null)
        {
            yield return "Battery storage slot details are required.";
            yield break;
        }

        if (slot.SlotNumber <= 0)
        {
            yield return "Battery storage slot number must be greater than zero.";
        }

        if (slot.BatteryCapacityKwh <= 0m)
        {
            yield return "Battery storage slot capacity in kWh must be greater than zero.";
        }
    }

    public static IEnumerable<string> ValidateSchedule(IReadOnlyList<OperatingWindowRequest>? schedule)
    {
        // Validate the weekly availability schedule that energy reservations depend on.
        if (schedule is null || schedule.Count == 0)
        {
            yield return "At least one operating window is required.";
            yield break;
        }

        var errors = new List<string>();

        foreach (var window in schedule)
        {
            if (window is null)
            {
                errors.Add("Operating window details are required.");
                continue;
            }

            if (!Enum.IsDefined(window.Day))
            {
                errors.Add("Operating window day of week is not supported.");
            }

            if (window.ClosesAt <= window.OpensAt)
            {
                errors.Add("An operating window must close after it opens.");
            }
        }

        if (HasOverlappingWindows(schedule))
        {
            errors.Add("Operating windows for the same day must not overlap.");
        }

        foreach (var error in errors.Distinct(StringComparer.Ordinal))
        {
            yield return error;
        }
    }

    public static IEnumerable<string> ValidateNearbyQuery(NearbyStationQuery? query)
    {
        // Validate the geo search parameters used by prosumer facing station discovery.
        if (query is null)
        {
            yield return "Nearby station search parameters are required.";
            yield break;
        }

        if (!GeoCoordinates.IsValidLatitude(query.Latitude))
        {
            yield return "Latitude must be between -90 and 90 degrees.";
        }

        if (!GeoCoordinates.IsValidLongitude(query.Longitude))
        {
            yield return "Longitude must be between -180 and 180 degrees.";
        }

        if (double.IsNaN(query.RadiusKilometers)
            || query.RadiusKilometers < MinimumRadiusKilometers
            || query.RadiusKilometers > MaximumRadiusKilometers)
        {
            yield return $"Search radius must be between {MinimumRadiusKilometers} and {MaximumRadiusKilometers} kilometers.";
        }

        if (query.MaxResults < 1 || query.MaxResults > MaximumNearbyResults)
        {
            yield return $"Maximum results must be between 1 and {MaximumNearbyResults}.";
        }
    }

    public static IEnumerable<string> ValidateStationQuery(SolarStationQuery? query)
    {
        // Validate list filters before passing them to persistence.
        if (query is null)
        {
            yield return "Station query parameters are required.";
            yield break;
        }

        if (query.Status.HasValue && !Enum.IsDefined(query.Status.Value))
        {
            yield return "Station status filter is not supported.";
        }

        if (query.PageNumber < 1)
        {
            yield return "Page number must be greater than zero.";
        }

        if (query.PageSize < 1)
        {
            yield return "Page size must be greater than zero.";
        }
    }

    private static bool HasOverlappingWindows(IReadOnlyList<OperatingWindowRequest> schedule)
    {
        // Compare only well formed windows so malformed ones report their own error.
        var windows = schedule
            .Where(window => window is not null && window.ClosesAt > window.OpensAt)
            .OrderBy(window => window.Day)
            .ThenBy(window => window.OpensAt)
            .ToArray();

        for (var index = 1; index < windows.Length; index++)
        {
            var previous = windows[index - 1];
            var current = windows[index];

            if (previous.Day == current.Day && current.OpensAt < previous.ClosesAt)
            {
                return true;
            }
        }

        return false;
    }
}
