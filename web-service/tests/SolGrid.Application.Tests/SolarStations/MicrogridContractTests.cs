/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: MicrogridContractTests.cs
 * Description: Verifies microgrid request validation and response contracts.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.SolarStations.Interfaces;
using SolGrid.Application.SolarStations.Requests;
using SolGrid.Application.SolarStations.Responses;
using SolGrid.Application.SolarStations.Validation;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using SolGrid.Domain.ValueObjects;
using Xunit;

namespace SolGrid.Application.Tests.SolarStations;

public sealed class MicrogridContractTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateStation_EmptySlots_IsValid()
    {
        // Permit a station with zero configured batteries using a derived non-negative count.
        var result = new CreateSolarStationRequestValidator().Validate(new CreateSolarStationRequest
        {
            Code = "ST-1",
            Name = "Station",
            AddressLine = "Colombo",
            Location = new() { Latitude = 6.9, Longitude = 79.8 },
            CapacityKw = 50m,
            Schedule = [new() { Day = DayOfWeek.Monday, OpensAt = new(8, 0), ClosesAt = new(17, 0) }]
        });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateStation_InvalidFields_AggregatesErrors()
    {
        // Return identity, GPS, capacity, slot, and weekly schedule errors in one result.
        var result = new CreateSolarStationRequestValidator().Validate(new CreateSolarStationRequest
        {
            Location = new() { Latitude = double.NaN, Longitude = 181 },
            Slots = [new() { SlotNumber = -1, BatteryCapacityKwh = -1, StartTime = Now, EndTime = Now }],
            Schedule = [new() { Day = (DayOfWeek)9, OpensAt = new(9, 0), ClosesAt = new(8, 0) }]
        });
        Assert.False(result.IsValid);
        Assert.Contains("Station code is required.", result.Errors);
        Assert.Contains("Station name is required.", result.Errors);
        Assert.Contains("Latitude must be between -90 and 90 degrees.", result.Errors);
        Assert.Contains("Longitude must be between -180 and 180 degrees.", result.Errors);
        Assert.Contains("Station capacity in kW must be greater than zero.", result.Errors);
        Assert.Contains("Battery storage slot number must be greater than zero.", result.Errors);
        Assert.Contains("Battery storage slot capacity in kWh must be greater than zero.", result.Errors);
        Assert.Contains("A booking slot must end after it starts.", result.Errors);
        Assert.Contains("Operating window day of week is not supported.", result.Errors);
    }

    [Fact]
    public void MissingRequests_AreValidationFailures()
    {
        // Handle missing deserialized request objects through the shared validation result.
        Assert.False(new CreateSolarStationRequestValidator().Validate(null!).IsValid);
        Assert.False(new UpdateSolarStationRequestValidator().Validate(null!).IsValid);
        Assert.False(new CreateBookingSlotRequestValidator().Validate(null!).IsValid);
        Assert.False(new UpdateBookingSlotRequestValidator().Validate(null!).IsValid);
        Assert.False(new UpdateSlotStatusRequestValidator().Validate(null!).IsValid);
        Assert.False(new UpdateStationScheduleRequestValidator().Validate(null!).IsValid);
        Assert.False(new SolarStationQueryValidator().Validate(null!).IsValid);
        Assert.False(new BookingSlotQueryValidator().Validate(null!).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SlotCreateAndUpdate_InvalidTimeRange_Reject(int hours)
    {
        // Enforce the same absolute time-range rule on both write contracts.
        Assert.False(new CreateBookingSlotRequestValidator().Validate(new()
        {
            SlotNumber = 1,
            BatteryCapacityKwh = 10m,
            StartTime = Now.ToOffset(TimeSpan.FromHours(-4)),
            EndTime = Now.AddHours(hours)
        }).IsValid);
        Assert.False(new UpdateBookingSlotRequestValidator().Validate(new()
        {
            BatteryCapacityKwh = 10m,
            StartTime = Now,
            EndTime = Now.AddHours(hours)
        }).IsValid);
    }

    [Fact]
    public void SlotCreateAndUpdate_ValidFields_Accept()
    {
        // Accept valid capacity and booking intervals without imposing reservation lead-time rules.
        Assert.True(new CreateBookingSlotRequestValidator().Validate(new()
        {
            SlotNumber = 1,
            BatteryCapacityKwh = 10m,
            StartTime = Now,
            EndTime = Now.AddHours(1)
        }).IsValid);
        Assert.True(new UpdateBookingSlotRequestValidator().Validate(new()
        {
            BatteryCapacityKwh = 20m,
            StartTime = Now,
            EndTime = Now.AddHours(2)
        }).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void SlotUpdate_NonPositiveCapacity_Rejects(int capacity)
    {
        // Reject unusable capacity even when the booking interval is otherwise valid.
        Assert.False(new UpdateBookingSlotRequestValidator().Validate(new()
        {
            BatteryCapacityKwh = capacity,
            StartTime = Now,
            EndTime = Now.AddHours(1)
        }).IsValid);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(999, false)]
    [InlineData((int)SlotStatus.Available, true)]
    [InlineData((int)SlotStatus.Reserved, true)]
    [InlineData((int)SlotStatus.Occupied, true)]
    [InlineData((int)SlotStatus.OutOfService, true)]
    public void StatusRequest_RequiresDefinedEnum(int status, bool valid)
    {
        // Validate enum membership before the entity checks the current-to-target transition.
        Assert.Equal(valid, new UpdateSlotStatusRequestValidator().Validate(new() { Status = (SlotStatus)status }).IsValid);
    }

    [Fact]
    public void Queries_RejectInvalidStatusPagingAndTimeFilters()
    {
        // Guard query contracts before they reach any database-specific implementation.
        var stations = new SolarStationQueryValidator().Validate(new()
        {
            Status = (StationStatus)999,
            PageNumber = 0,
            PageSize = -1
        });
        Assert.Equal(3, stations.Errors.Count);
        var slots = new BookingSlotQueryValidator().Validate(new()
        {
            StationId = " ",
            Status = (SlotStatus)999,
            PageNumber = 0,
            PageSize = -1,
            From = Now,
            To = Now
        });
        Assert.Equal(5, slots.Errors.Count);
        Assert.True(new SolarStationQueryValidator().Validate(new()).IsValid);
        Assert.True(new BookingSlotQueryValidator().Validate(new()).IsValid);
        Assert.True(new BookingSlotQueryValidator().Validate(new() { From = Now }).IsValid);
        Assert.True(new BookingSlotQueryValidator().Validate(new() { To = Now }).IsValid);
    }

    [Fact]
    public void SlotCollection_RejectsMissingEntriesAndDuplicateNumbers()
    {
        // Prevent malformed nested slot lists from bypassing individual validators.
        var slot = new CreateBookingSlotRequest
        {
            SlotNumber = 1,
            BatteryCapacityKwh = 10m,
            StartTime = Now,
            EndTime = Now.AddHours(1)
        };
        Assert.NotEmpty(SolarStationValidationRules.ValidateSlots(null));
        Assert.NotEmpty(SolarStationValidationRules.ValidateSlots([slot, null!]));
        Assert.NotEmpty(SolarStationValidationRules.ValidateSlots([slot, slot]));
    }

    [Fact]
    public void ScheduleValidation_RejectsOverlapButAllowsAdjacentWindows()
    {
        // Apply the same half-open window interpretation in application and domain validation.
        var first = new OperatingWindowRequest { Day = DayOfWeek.Monday, OpensAt = new(8, 0), ClosesAt = new(10, 0) };
        var overlap = new OperatingWindowRequest { Day = DayOfWeek.Monday, OpensAt = new(9, 0), ClosesAt = new(11, 0) };
        var adjacent = new OperatingWindowRequest { Day = DayOfWeek.Monday, OpensAt = new(10, 0), ClosesAt = new(12, 0) };
        Assert.NotEmpty(SolarStationValidationRules.ValidateSchedule([first, overlap]));
        Assert.Empty(SolarStationValidationRules.ValidateSchedule([first, adjacent]));
    }

    [Fact]
    public void SlotScheduleValidation_RequiresCoverageByOperatingWindows()
    {
        // Reject booking intervals that fall outside the station's weekly schedule.
        var schedule = new[] { OperatingWindow.Create(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0)) };
        var mondayStart = new DateTimeOffset(2026, 9, 14, 8, 0, 0, TimeSpan.Zero);

        Assert.Empty(SolarStationValidationRules.ValidateSlotAgainstSchedule(
            schedule, mondayStart, mondayStart.AddHours(2)));
        Assert.Contains(
            "Booking slot times must fall within the station operating schedule.",
            SolarStationValidationRules.ValidateSlotAgainstSchedule(
                schedule, mondayStart.AddDays(1), mondayStart.AddDays(1).AddHours(2)));
    }

    [Theory]
    [InlineData(SlotStatus.Available, true, true)]
    [InlineData(SlotStatus.Reserved, true, false)]
    [InlineData(SlotStatus.Occupied, true, false)]
    [InlineData(SlotStatus.OutOfService, false, false)]
    public void SlotResponse_PreservesOwnershipTimesStatusAndAudit(SlotStatus status, bool active, bool available)
    {
        // Expose only domain values needed by clients without database-specific types.
        var slot = EnergyBookingSlot.Restore("slot-1", "station-1", 1, 10m,
            Now, Now.AddHours(1), status, Now, Now.AddMinutes(1));
        var response = SolarStationResponseMapper.ToResponse(slot);
        Assert.Equal(slot.Id, response.Id);
        Assert.Equal(slot.StationId, response.StationId);
        Assert.Equal(slot.StartTime, response.StartTime);
        Assert.Equal(slot.EndTime, response.EndTime);
        Assert.Equal(status, response.Status);
        Assert.Equal(active, response.IsActive);
        Assert.Equal(available, response.IsAvailable);
        Assert.Equal(slot.CreatedAt, response.CreatedAt);
        Assert.Equal(slot.UpdatedAt, response.UpdatedAt);
    }
}
