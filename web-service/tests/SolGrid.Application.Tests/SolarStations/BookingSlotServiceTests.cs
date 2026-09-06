/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: BookingSlotServiceTests.cs
 * Description: Verifies energy booking slot management business use cases.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Common.Identity;
using SolGrid.Application.Common.Models;
using SolGrid.Application.SolarStations.Interfaces;
using SolGrid.Application.SolarStations.Requests;
using SolGrid.Application.SolarStations.Services;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using SolGrid.Domain.ValueObjects;
using Xunit;

namespace SolGrid.Application.Tests.SolarStations;

public sealed class BookingSlotServiceTests
{
    private static readonly DateTimeOffset CurrentTime = new(2026, 9, 16, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset MondayStart = new(2026, 9, 14, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateBookingSlotAsync_WithValidRequest_CreatesAvailableSlot()
    {
        // Verify a valid slot is persisted against its owning active station.
        var station = CreateStation();
        var stations = new InMemorySolarStationRepository(station);
        var slots = new InMemoryBookingSlotRepository();
        var service = CreateService(stations, slots);

        var response = await service.CreateBookingSlotAsync(station.Id, CreateRequest());
        var persistedSlot = await slots.GetByIdAsync(response.Id);

        Assert.NotNull(persistedSlot);
        Assert.Equal(station.Id, response.StationId);
        Assert.Equal(1, response.SlotNumber);
        Assert.Equal(12.5m, response.BatteryCapacityKwh);
        Assert.Equal(MondayStart, response.StartTime);
        Assert.Equal(MondayStart.AddHours(2), response.EndTime);
        Assert.Equal(SlotStatus.Available, response.Status);
        Assert.True(response.IsActive);
        Assert.True(response.IsAvailable);
        Assert.Equal(CurrentTime, response.CreatedAt);
        Assert.Equal(1, station.TotalSlotCount);
    }

    [Fact]
    public async Task CreateBookingSlotAsync_WithMissingStation_ThrowsNotFound()
    {
        // Verify slots cannot be created against an unknown station.
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateBookingSlotAsync("missing-station", CreateRequest()));
    }

    [Fact]
    public async Task CreateBookingSlotAsync_WithInactiveStation_ThrowsConflict()
    {
        // Verify new slots are rejected while the owning station is inactive.
        var station = CreateStation();
        station.Deactivate(CurrentTime);
        var service = CreateService(new InMemorySolarStationRepository(station));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateBookingSlotAsync(station.Id, CreateRequest()));

        Assert.Equal("Station is not active for booking slot management.", exception.Message);
    }

    [Fact]
    public async Task CreateBookingSlotAsync_WithInvalidTimeRange_ThrowsValidation()
    {
        // Verify reversed booking intervals are rejected before persistence.
        var station = CreateStation();
        var service = CreateService(new InMemorySolarStationRepository(station));

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateBookingSlotAsync(station.Id, CreateRequest(endTime: MondayStart)));

        Assert.Contains("A booking slot must end after it starts.", exception.Errors);
    }

    [Fact]
    public async Task CreateBookingSlotAsync_OutsideOperatingSchedule_ThrowsValidation()
    {
        // Verify slot times must fall inside the station's weekly operating windows.
        var station = CreateStation();
        var service = CreateService(new InMemorySolarStationRepository(station));

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateBookingSlotAsync(station.Id, CreateRequest(
                startTime: MondayStart.AddDays(1),
                endTime: MondayStart.AddDays(1).AddHours(2))));

        Assert.Contains("Booking slot times must fall within the station operating schedule.", exception.Errors);
    }

    [Fact]
    public async Task CreateBookingSlotAsync_WithDuplicateSlotNumber_ThrowsConflict()
    {
        // Verify per-station slot numbers remain unique.
        var station = CreateStation();
        var stations = new InMemorySolarStationRepository(station);
        var service = CreateService(stations);

        await service.CreateBookingSlotAsync(station.Id, CreateRequest());

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateBookingSlotAsync(station.Id, CreateRequest(slotNumber: 1)));

        Assert.Equal("Slot number 1 is already used by this station.", exception.Message);
    }

    [Fact]
    public async Task CreateBookingSlotAsync_WithOverlappingIntervalAndDifferentSlotNumber_Succeeds()
    {
        // Distinct battery slots may share a time window because uniqueness is by slot number.
        var station = CreateStation();
        var service = CreateService(new InMemorySolarStationRepository(station));

        await service.CreateBookingSlotAsync(station.Id, CreateRequest(slotNumber: 1));
        var secondSlot = await service.CreateBookingSlotAsync(station.Id, CreateRequest(slotNumber: 2));

        Assert.Equal(2, secondSlot.SlotNumber);
        Assert.Equal(2, station.TotalSlotCount);
    }

    [Fact]
    public async Task UpdateBookingSlotAsync_WithEditableFields_UpdatesSlot()
    {
        // Verify capacity and times can change without reassigning identity or station ownership.
        var station = CreateStation();
        var stations = new InMemorySolarStationRepository(station);
        var slots = new InMemoryBookingSlotRepository();
        var service = CreateService(stations, slots);
        var created = await service.CreateBookingSlotAsync(station.Id, CreateRequest());

        var response = await service.UpdateBookingSlotAsync(created.Id, new UpdateBookingSlotRequest
        {
            BatteryCapacityKwh = 20m,
            StartTime = MondayStart.AddHours(1),
            EndTime = MondayStart.AddHours(3)
        });

        Assert.Equal(created.Id, response.Id);
        Assert.Equal(station.Id, response.StationId);
        Assert.Equal(1, response.SlotNumber);
        Assert.Equal(20m, response.BatteryCapacityKwh);
        Assert.Equal(MondayStart.AddHours(1), response.StartTime);
        Assert.Equal(MondayStart.AddHours(3), response.EndTime);
    }

    [Fact]
    public async Task ActivateAndDeactivateBookingSlotAsync_CyclesAvailability()
    {
        // Verify explicit lifecycle operations withdraw and restore a slot without deleting it.
        var station = CreateStation();
        var slots = new InMemoryBookingSlotRepository();
        var service = CreateService(new InMemorySolarStationRepository(station), slots);
        var created = await service.CreateBookingSlotAsync(station.Id, CreateRequest());

        await service.DeactivateBookingSlotAsync(created.Id);
        var deactivated = await slots.GetByIdAsync(created.Id);
        var deactivatedStatus = deactivated!.Status;
        var deactivatedIsAvailable = deactivated.IsAvailable;

        await service.ActivateBookingSlotAsync(created.Id);
        var activated = await slots.GetByIdAsync(created.Id);

        Assert.Equal(SlotStatus.OutOfService, deactivatedStatus);
        Assert.False(deactivatedIsActive);
        Assert.False(deactivatedIsAvailable);
        Assert.Equal(SlotStatus.Available, activated!.Status);
        Assert.True(activated.IsActive);
        Assert.True(activated.IsAvailable);
        Assert.NotNull(await slots.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task DeactivateBookingSlotAsync_WithActiveReservation_ThrowsConflict()
    {
        // Verify live reservation references block deactivation rather than physical deletion.
        var station = CreateStation();
        var service = CreateService(
            new InMemorySolarStationRepository(station),
            activeReservationForSlot: true);
        var created = await service.CreateBookingSlotAsync(station.Id, CreateRequest());

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.DeactivateBookingSlotAsync(created.Id));

        Assert.Equal("A slot with active reservations cannot be changed.", exception.Message);
    }

    [Fact]
    public async Task ActivateBookingSlotAsync_WhenAlreadyAvailable_ThrowsConflict()
    {
        // Verify illegal status transitions are rejected from the current loaded state.
        var station = CreateStation();
        var service = CreateService(new InMemorySolarStationRepository(station));
        var created = await service.CreateBookingSlotAsync(station.Id, CreateRequest());

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.ActivateBookingSlotAsync(created.Id));

        Assert.Equal("A slot in 'Available' state cannot be returned to service.", exception.Message);
    }

    [Fact]
    public async Task CreateBookingSlotAsync_WithGridOperatorRole_ThrowsForbidden()
    {
        // Verify administrative slot creation is not open to GridOperator callers.
        var station = CreateStation();
        var service = CreateService(new InMemorySolarStationRepository(station), role: UserRole.GridOperator);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CreateBookingSlotAsync(station.Id, CreateRequest()));
    }

    [Fact]
    public async Task GetBookingSlotsAsync_FiltersByStationAndStatus()
    {
        // Verify station-scoped listing supports status and pagination filters.
        var station = CreateStation();
        var service = CreateService(new InMemorySolarStationRepository(station));
        var first = await service.CreateBookingSlotAsync(station.Id, CreateRequest(slotNumber: 1));
        await service.CreateBookingSlotAsync(station.Id, CreateRequest(slotNumber: 2));
        await service.DeactivateBookingSlotAsync(first.Id);

        var page = await service.GetBookingSlotsAsync(new BookingSlotQuery
        {
            StationId = station.Id,
            Status = SlotStatus.OutOfService,
            PageNumber = 1,
            PageSize = 10
        });

        var slot = Assert.Single(page.Items);
        Assert.Equal(first.Id, slot.Id);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task ReservationBookingSlotReadService_ExposesAvailabilityContract()
    {
        // Verify reservations can check existence, ownership, activity, and availability without Mongo types.
        var station = CreateStation();
        var slots = new InMemoryBookingSlotRepository();
        var service = CreateService(new InMemorySolarStationRepository(station), slots);
        var created = await service.CreateBookingSlotAsync(station.Id, CreateRequest());
        var readService = new ReservationBookingSlotReadService(slots);

        var available = await readService.GetByIdAsync(created.Id);
        await service.DeactivateBookingSlotAsync(created.Id);
        var inactive = await readService.GetByIdAsync(created.Id);
        var missing = await readService.GetByIdAsync("missing-slot");

        Assert.Equal(created.Id, available!.Id);
        Assert.Equal(station.Id, available.StationId);
        Assert.True(available.IsActive);
        Assert.True(available.IsAvailable);
        Assert.False(inactive!.IsActive);
        Assert.False(inactive.IsAvailable);
        Assert.Null(missing);
    }

    private static BookingSlotService CreateService(
        InMemorySolarStationRepository? stations = null,
        InMemoryBookingSlotRepository? slots = null,
        bool activeReservationForSlot = false,
        UserRole role = UserRole.Backoffice)
    {
        // Create BookingSlotService with deterministic test dependencies.
        return new BookingSlotService(
            stations ?? new InMemorySolarStationRepository(),
            slots ?? new InMemoryBookingSlotRepository(),
            new FakeStationReservationLookup(activeReservationForSlot),
            new FakeCurrentUserContext("backoffice-id", role),
            new FixedTimeProvider(CurrentTime));
    }

    private static CreateBookingSlotRequest CreateRequest(
        int slotNumber = 1,
        decimal capacity = 12.5m,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null)
    {
        // Build a valid slot create request aligned to the test station schedule.
        return new CreateBookingSlotRequest
        {
            SlotNumber = slotNumber,
            BatteryCapacityKwh = capacity,
            StartTime = startTime ?? MondayStart,
            EndTime = endTime ?? MondayStart.AddHours(2)
        };
    }

    private static SolarStation CreateStation()
    {
        // Create an active station whose Monday window covers the test booking interval.
        return SolarStation.Create(
            "station-1",
            "ST-1",
            "Colombo North",
            "Colombo",
            GeoCoordinates.Create(6.9271, 79.8612),
            50m,
            [],
            [OperatingWindow.Create(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0))],
            CurrentTime);
    }

    private sealed class FakeCurrentUserContext : ICurrentUserContext
    {
        public FakeCurrentUserContext(string userId, UserRole role)
        {
            // Store trusted current-user identity values for authorization tests.
            UserId = userId;
            Role = role;
        }

        public bool IsAuthenticated => true;

        public string? UserId { get; }

        public UserRole? Role { get; }
    }

    private sealed class FakeStationReservationLookup : IStationReservationLookup
    {
        private readonly bool activeReservationForSlot;

        public FakeStationReservationLookup(bool activeReservationForSlot)
        {
            // Store the reservation-module answer used by slot lifecycle tests.
            this.activeReservationForSlot = activeReservationForSlot;
        }

        public Task<int> CountActiveReservationsAsync(string stationId, CancellationToken cancellationToken = default)
        {
            // Slot tests do not deactivate stations.
            return Task.FromResult(0);
        }

        public Task<bool> HasActiveReservationForSlotAsync(
            string bookingSlotId, CancellationToken cancellationToken = default)
        {
            // Return the configured slot occupancy without reservation persistence.
            return Task.FromResult(activeReservationForSlot);
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            // Freeze application time for deterministic audit values.
            this.utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            // Return the frozen UTC timestamp used by booking slot tests.
            return utcNow;
        }
    }

    private sealed class InMemorySolarStationRepository : ISolarStationRepository
    {
        private readonly List<SolarStation> stations;

        public InMemorySolarStationRepository(params SolarStation[] stations)
        {
            // Store test stations in memory.
            this.stations = stations.ToList();
        }

        public Task<SolarStation?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            // Find a station by id in memory.
            return Task.FromResult(stations.FirstOrDefault(station => station.Id == id.Trim()));
        }

        public Task<SolarStation?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            // Find a station by normalized code in memory.
            var normalizedCode = code.Trim().ToUpperInvariant();
            return Task.FromResult(stations.FirstOrDefault(station => station.Code == normalizedCode));
        }

        public Task<IReadOnlyList<SolarStation>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // Return all in-memory stations.
            return Task.FromResult<IReadOnlyList<SolarStation>>(stations);
        }

        public Task<PagedResult<SolarStation>> GetPagedAsync(
            SolarStationQuery query, CancellationToken cancellationToken = default)
        {
            // Return all in-memory stations for unused station-list calls.
            return Task.FromResult(new PagedResult<SolarStation>
            {
                Items = stations,
                TotalCount = stations.Count,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            });
        }

        public Task<IReadOnlyList<SolarStation>> GetNearbyAsync(
            NearbyStationQuery query, CancellationToken cancellationToken = default)
        {
            // Nearby discovery is not exercised by booking slot tests.
            return Task.FromResult<IReadOnlyList<SolarStation>>(stations);
        }

        public Task<bool> ExistsByCodeAsync(
            string code,
            string? excludingStationId = null,
            CancellationToken cancellationToken = default)
        {
            // Check whether a normalized code exists in memory.
            var normalizedCode = code.Trim().ToUpperInvariant();
            return Task.FromResult(stations.Any(station =>
                station.Code == normalizedCode && station.Id != excludingStationId));
        }

        public Task AddAsync(SolarStation station, CancellationToken cancellationToken = default)
        {
            // Add a station to the in-memory list.
            stations.Add(station);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SolarStation station, CancellationToken cancellationToken = default)
        {
            // Replace is not required because domain stations are updated in place for tests.
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryBookingSlotRepository : IBookingSlotRepository
    {
        private readonly List<EnergyBookingSlot> slots = [];

        public Task<EnergyBookingSlot?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            // Find a slot by id in memory.
            return Task.FromResult(slots.FirstOrDefault(slot => slot.Id == id.Trim()));
        }

        public Task<PagedResult<EnergyBookingSlot>> GetPagedAsync(
            BookingSlotQuery query, CancellationToken cancellationToken = default)
        {
            // Return filtered in-memory slots for service tests.
            IEnumerable<EnergyBookingSlot> queryableSlots = slots;

            if (!string.IsNullOrWhiteSpace(query.StationId))
            {
                queryableSlots = queryableSlots.Where(slot => slot.StationId == query.StationId.Trim());
            }

            if (query.Status.HasValue)
            {
                queryableSlots = queryableSlots.Where(slot => slot.Status == query.Status.Value);
            }

            if (query.From.HasValue)
            {
                queryableSlots = queryableSlots.Where(slot => slot.EndTime > query.From.Value);
            }

            if (query.To.HasValue)
            {
                queryableSlots = queryableSlots.Where(slot => slot.StartTime < query.To.Value);
            }

            var items = queryableSlots
                .OrderBy(slot => slot.StartTime)
                .ThenBy(slot => slot.SlotNumber)
                .ToArray();

            return Task.FromResult(new PagedResult<EnergyBookingSlot>
            {
                Items = items,
                TotalCount = items.Length,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            });
        }

        public Task AddAsync(EnergyBookingSlot slot, CancellationToken cancellationToken = default)
        {
            // Add a slot to the in-memory list.
            slots.Add(slot);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(EnergyBookingSlot slot, CancellationToken cancellationToken = default)
        {
            // Replace is not required because domain slots are updated in place for tests.
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id, CancellationToken cancellationToken = default)
        {
            // Remove a slot from in-memory storage.
            slots.RemoveAll(slot => slot.Id == id.Trim());
            return Task.CompletedTask;
        }

        public Task<bool> ExistsBySlotNumberAsync(
            string stationId,
            int slotNumber,
            string? excludingSlotId = null,
            CancellationToken cancellationToken = default)
        {
            // Check whether a station already has the requested slot number.
            return Task.FromResult(slots.Any(slot =>
                slot.StationId == stationId.Trim()
                && slot.SlotNumber == slotNumber
                && slot.Id != excludingSlotId));
        }

        public Task<bool> HasOverlappingIntervalAsync(
            string stationId,
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            string? excludingSlotId = null,
            CancellationToken cancellationToken = default)
        {
            // Detect overlapping in-memory intervals for the same station.
            return Task.FromResult(slots.Any(slot =>
                slot.StationId == stationId.Trim()
                && slot.Id != excludingSlotId
                && slot.StartTime < endTime
                && slot.EndTime > startTime));
        }
    }
}
