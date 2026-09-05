/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: SolarStationServiceTests.cs
 * Description: Verifies microgrid solar station management business use cases.
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

public sealed class SolarStationServiceTests
{
    private static readonly DateTimeOffset CurrentTime = new(2026, 9, 16, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateStationAsync_WithValidRequest_CreatesActiveStation()
    {
        // Verify a valid station is persisted with normalized code and active status.
        var repository = new InMemorySolarStationRepository();
        var service = CreateService(repository);

        var response = await service.CreateStationAsync(CreateRequest());
        var persistedStation = await repository.GetByIdAsync(response.Id);

        Assert.NotNull(persistedStation);
        Assert.Equal("ST-1", response.Code);
        Assert.Equal("Colombo North", response.Name);
        Assert.Equal(50m, response.CapacityKw);
        Assert.Equal(StationStatus.Active, response.Status);
        Assert.Equal(6.9271, response.Location.Latitude);
        Assert.Equal(79.8612, response.Location.Longitude);
        Assert.Equal(CurrentTime, response.CreatedAt);
        Assert.Equal(CurrentTime, persistedStation!.UpdatedAt);
    }

    [Fact]
    public async Task CreateStationAsync_WithInvalidLatitude_ThrowsValidation()
    {
        // Verify out-of-range latitude is rejected before persistence.
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateStationAsync(
            CreateRequest(location: new GeoCoordinatesRequest { Latitude = 91, Longitude = 79.8612 })));

        Assert.Contains("Latitude must be between -90 and 90 degrees.", exception.Errors);
    }

    [Fact]
    public async Task CreateStationAsync_WithInvalidLongitude_ThrowsValidation()
    {
        // Verify out-of-range longitude is rejected before persistence.
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateStationAsync(
            CreateRequest(location: new GeoCoordinatesRequest { Latitude = 6.9271, Longitude = 181 })));

        Assert.Contains("Longitude must be between -180 and 180 degrees.", exception.Errors);
    }

    [Fact]
    public async Task CreateStationAsync_WithInvalidCapacity_ThrowsValidation()
    {
        // Verify non-positive generation capacity is rejected before persistence.
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateStationAsync(
            CreateRequest(capacityKw: 0m)));

        Assert.Contains("Station capacity in kW must be greater than zero.", exception.Errors);
    }

    [Fact]
    public async Task CreateStationAsync_WithDuplicateStationCode_ThrowsConflict()
    {
        // Verify duplicate normalized station codes are rejected.
        var repository = new InMemorySolarStationRepository(CreateStation("existing-id", "ST-1"));
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateStationAsync(CreateRequest(code: " st-1 ")));
    }

    [Fact]
    public async Task UpdateStationAsync_WithEditableFields_UpdatesStation()
    {
        // Verify descriptive fields and capacity can be updated without changing identity.
        var station = CreateStation("station-id", "ST-1");
        var repository = new InMemorySolarStationRepository(station);
        var service = CreateService(repository);

        var response = await service.UpdateStationAsync("station-id", new UpdateSolarStationRequest
        {
            Code = "st-2",
            Name = "Updated Station",
            AddressLine = "Kandy",
            Location = new GeoCoordinatesRequest { Latitude = 7.29, Longitude = 80.63 },
            CapacityKw = 75m
        });

        Assert.Equal("station-id", response.Id);
        Assert.Equal("ST-2", response.Code);
        Assert.Equal("Updated Station", response.Name);
        Assert.Equal(75m, response.CapacityKw);
        Assert.Equal(StationStatus.Active, response.Status);
    }

    [Fact]
    public async Task ActivateStationAsync_ReactivatesInactiveStation()
    {
        // Verify activation restores an inactive station through the domain transition.
        var station = CreateStation("station-id", "ST-1");
        station.Deactivate(CurrentTime);
        var repository = new InMemorySolarStationRepository(station);
        var service = CreateService(repository);

        await service.ActivateStationAsync("station-id");

        Assert.Equal(StationStatus.Active, station.Status);
        Assert.True(station.IsActive);
    }

    [Fact]
    public async Task DeactivateStationAsync_WithoutActiveReservations_DeactivatesStation()
    {
        // Verify deactivation succeeds when the reservation lookup reports no live bookings.
        var station = CreateStation("station-id", "ST-1");
        var repository = new InMemorySolarStationRepository(station);
        var service = CreateService(repository, activeReservationCount: 0);

        await service.DeactivateStationAsync("station-id");

        Assert.Equal(StationStatus.Inactive, station.Status);
        Assert.False(station.IsActive);
    }

    [Fact]
    public async Task DeactivateStationAsync_WithActiveReservations_ThrowsConflict()
    {
        // Verify deactivation is blocked while the reservation module reports live bookings.
        var station = CreateStation("station-id", "ST-1");
        var repository = new InMemorySolarStationRepository(station);
        var service = CreateService(repository, activeReservationCount: 2);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => service.DeactivateStationAsync("station-id"));

        Assert.Equal("A station with active reservations cannot be deactivated.", exception.Message);
        Assert.Equal(StationStatus.Active, station.Status);
    }

    [Fact]
    public async Task CreateStationAsync_WithGridOperatorRole_ThrowsForbidden()
    {
        // Verify administrative station creation is not open to GridOperator callers.
        var service = CreateService(role: UserRole.GridOperator);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.CreateStationAsync(CreateRequest()));
    }

    [Fact]
    public async Task GetStationsAsync_WithStatusAndSearchFilters_ReturnsMatchingStations()
    {
        // Verify station lists support useful status, search, and pagination filters.
        var matchingStation = CreateStation("first-id", "ST-NORTH", "Colombo North Hub");
        matchingStation.Deactivate(CurrentTime);
        var otherStation = CreateStation("second-id", "ST-SOUTH", "Galle South Hub");
        var repository = new InMemorySolarStationRepository(matchingStation, otherStation);
        var service = CreateService(repository);

        var response = await service.GetStationsAsync(new SolarStationQuery
        {
            SearchText = "north",
            Status = StationStatus.Inactive,
            PageNumber = 1,
            PageSize = 10
        });

        var station = Assert.Single(response.Items);
        Assert.Equal("ST-NORTH", station.Code);
        Assert.Equal(1, response.TotalCount);
        Assert.Equal(1, response.PageNumber);
        Assert.Equal(10, response.PageSize);
    }

    private static SolarStationService CreateService(
        InMemorySolarStationRepository? repository = null,
        int activeReservationCount = 0,
        UserRole role = UserRole.Backoffice)
    {
        // Create SolarStationService with deterministic test dependencies.
        return new SolarStationService(
            repository ?? new InMemorySolarStationRepository(),
            new FakeStationReservationLookup(activeReservationCount),
            new FakeCurrentUserContext("backoffice-id", role),
            new FixedTimeProvider(CurrentTime));
    }

    private static CreateSolarStationRequest CreateRequest(
        string code = "ST-1",
        decimal capacityKw = 50m,
        GeoCoordinatesRequest? location = null)
    {
        // Build a valid station create request with assignment-required data areas.
        return new CreateSolarStationRequest
        {
            Code = code,
            Name = "Colombo North",
            AddressLine = "Colombo",
            Location = location ?? new GeoCoordinatesRequest { Latitude = 6.9271, Longitude = 79.8612 },
            CapacityKw = capacityKw,
            Schedule =
            [
                new OperatingWindowRequest
                {
                    Day = DayOfWeek.Monday,
                    OpensAt = new TimeOnly(8, 0),
                    ClosesAt = new TimeOnly(17, 0)
                }
            ]
        };
    }

    private static SolarStation CreateStation(string id, string code, string name = "Station")
    {
        // Create a valid domain station for service tests.
        return SolarStation.Create(
            id,
            code,
            name,
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
        private readonly int activeReservationCount;

        public FakeStationReservationLookup(int activeReservationCount)
        {
            // Store the reservation-module answer used by deactivation tests.
            this.activeReservationCount = activeReservationCount;
        }

        public Task<int> CountActiveReservationsAsync(string stationId, CancellationToken cancellationToken = default)
        {
            // Return the configured active-reservation count without reservation persistence.
            return Task.FromResult(activeReservationCount);
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
            // Return the frozen UTC timestamp used by station tests.
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
            SolarStationQuery query,
            CancellationToken cancellationToken = default)
        {
            // Return filtered in-memory stations for service tests.
            IEnumerable<SolarStation> queryableStations = stations;

            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                var searchText = query.SearchText.Trim();
                queryableStations = queryableStations.Where(station =>
                    station.Code.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || station.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || station.AddressLine.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            if (query.Status.HasValue)
            {
                queryableStations = queryableStations.Where(station => station.Status == query.Status.Value);
            }

            if (query.HasAvailableSlots.HasValue)
            {
                queryableStations = queryableStations.Where(station =>
                    query.HasAvailableSlots.Value
                        ? station.AvailableSlotCount > 0
                        : station.AvailableSlotCount == 0);
            }

            var items = queryableStations.ToArray();
            return Task.FromResult(new PagedResult<SolarStation>
            {
                Items = items,
                TotalCount = items.Length,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            });
        }

        public Task<IReadOnlyList<SolarStation>> GetNearbyAsync(
            NearbyStationQuery query,
            CancellationToken cancellationToken = default)
        {
            // Rank in-memory stations by great-circle distance for discovery tests.
            var origin = GeoCoordinates.Create(query.Latitude, query.Longitude);
            var stationsInRange = stations
                .Where(station => !query.ActiveOnly || station.IsActive)
                .Where(station => station.DistanceInKilometersFrom(origin) <= query.RadiusKilometers)
                .OrderBy(station => station.DistanceInKilometersFrom(origin))
                .Take(query.MaxResults)
                .ToArray();

            return Task.FromResult<IReadOnlyList<SolarStation>>(stationsInRange);
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
}
