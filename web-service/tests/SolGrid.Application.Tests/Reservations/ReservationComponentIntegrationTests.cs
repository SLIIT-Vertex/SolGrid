/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationComponentIntegrationTests.cs
 * Description: Verifies reservation integration with prosumer and microgrid application contracts.
 * Contributor: Dilshan Yapa S Y C T
 */

using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Common.Identity;
using SolGrid.Application.Common.Models;
using SolGrid.Application.Prosumers.Interfaces;
using SolGrid.Application.Prosumers.Services;
using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Application.Reservations.Requests;
using SolGrid.Application.Reservations.Services;
using SolGrid.Application.SolarStations.Interfaces;
using SolGrid.Application.SolarStations.Services;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using SolGrid.Domain.ValueObjects;
using Xunit;

namespace SolGrid.Application.Tests.Reservations;

public sealed class ReservationComponentIntegrationTests
{
    private static readonly DateTimeOffset CurrentTime = new(2026, 9, 16, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateReservationAsync_WithActiveProsumerAndBookableSlot_CreatesReservation()
    {
        // Verify ESR consumes the real Prosumer, station, and slot read adapters successfully.
        var fixture = CreateFixture();

        var response = await fixture.ReservationService.CreateReservationAsync(fixture.CreateRequest());

        Assert.Equal(fixture.Prosumer.Nic, response.ProsumerId);
        Assert.Equal(fixture.Station.Id, response.StationId);
        Assert.Equal(fixture.Slot.Id, response.BookingSlotId);
        Assert.Equal(ReservationStatus.Pending, response.Status);
    }

    [Fact]
    public async Task CreateReservationAsync_WithInactiveProsumer_RejectsReservation()
    {
        // Verify the Prosumer adapter exposes lifecycle eligibility to ESR.
        var fixture = CreateFixture(activeProsumer: false);

        await Assert.ThrowsAsync<ConflictException>(() => fixture.ReservationService.CreateReservationAsync(fixture.CreateRequest()));
    }

    [Fact]
    public async Task CreateReservationAsync_WithInactiveStation_RejectsReservation()
    {
        // Verify the station adapter prevents reservations at inactive microgrid nodes.
        var fixture = CreateFixture(activeStation: false);

        await Assert.ThrowsAsync<ConflictException>(() => fixture.ReservationService.CreateReservationAsync(fixture.CreateRequest()));
    }

    [Fact]
    public async Task CreateReservationAsync_WhenSlotBelongsToDifferentStation_RejectsReservation()
    {
        // Verify ESR compares the slot's persisted station identifier to the requested station identifier.
        var fixture = CreateFixture(slotStationId: "station-2");

        await Assert.ThrowsAsync<ValidationException>(() => fixture.ReservationService.CreateReservationAsync(fixture.CreateRequest()));
    }

    [Fact]
    public async Task CreateReservationAsync_WithUnavailableSlot_RejectsReservation()
    {
        // Verify the shared slot availability state is authoritative for ESR creates.
        var fixture = CreateFixture(slotAvailable: false);

        await Assert.ThrowsAsync<ConflictException>(() => fixture.ReservationService.CreateReservationAsync(fixture.CreateRequest()));
    }

    [Fact]
    public async Task DeactivateStationAsync_WithActiveReservation_IsBlockedByReservationLookup()
    {
        // Verify station lifecycle uses the ESR repository contract rather than bypassing reservations.
        var fixture = CreateFixture();
        await fixture.ReservationService.CreateReservationAsync(fixture.CreateRequest());
        var stationService = new SolarStationService(
            fixture.Stations,
            fixture.Slots,
            new ReservationStationLookup(fixture.Reservations),
            new TestCurrentUserContext("backoffice-1", UserRole.Backoffice),
            new FixedTimeProvider(CurrentTime));

        await Assert.ThrowsAsync<ConflictException>(() => stationService.DeactivateStationAsync(fixture.Station.Id));
    }

    [Fact]
    public async Task CreateReservationAsync_AsBackoffice_CanonicalizesProsumerReferenceFromProsumerContract()
    {
        // Verify an administrative request cannot persist a non-canonical representation of a NIC.
        var fixture = CreateFixture(nic: "199012345V", callerRole: UserRole.Backoffice);

        var response = await fixture.ReservationService.CreateReservationAsync(fixture.CreateRequest(prosumerId: "199012345v"));

        Assert.Equal("199012345V", response.ProsumerId);
    }

    [Fact]
    public async Task GetMyReservationsAsync_WithWebUserRole_IsForbidden()
    {
        // Verify web-user JWT subjects cannot be misinterpreted as prosumer reservation ownership.
        var fixture = CreateFixture(callerRole: UserRole.GridOperator);

        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.ReservationService.GetMyReservationsAsync(new ReservationQuery()));
    }

    private static Fixture CreateFixture(
        bool activeProsumer = true,
        bool activeStation = true,
        bool slotAvailable = true,
        string? slotStationId = null,
        string nic = "199012345678",
        UserRole? callerRole = null)
    {
        // Build real cross-component adapters over simple in-memory application repositories.
        var prosumer = Prosumer.Create(nic, "Nimal", "Perera", "nimal@example.com", null, "hash", CurrentTime);
        if (activeProsumer)
        {
            prosumer.Activate(CurrentTime);
        }

        var station = SolarStation.Create(
            "station-1", "ST-1", "Colombo", "Colombo", GeoCoordinates.Create(6.9271, 79.8612), 50m, [],
            [OperatingWindow.Create(DayOfWeek.Tuesday, new TimeOnly(8, 0), new TimeOnly(18, 0))], CurrentTime);
        if (!activeStation)
        {
            station.Deactivate(CurrentTime);
        }

        var slot = EnergyBookingSlot.Create(
            "slot-1", slotStationId ?? station.Id, 1, 12m, CurrentTime.AddHours(2), CurrentTime.AddHours(3), CurrentTime);
        if (!slotAvailable)
        {
            slot.TakeOutOfService(CurrentTime);
        }

        var prosumers = new TestProsumerRepository(prosumer);
        var stations = new TestStationRepository(station);
        var slots = new TestSlotRepository(slot);
        var reservations = new TestReservationRepository();
        var reservationService = new ReservationService(
            reservations,
            new ReservationProsumerReadService(prosumers),
            new ReservationStationReadService(stations),
            new ReservationBookingSlotReadService(slots, new FixedTimeProvider(CurrentTime)),
            new TestQrTokenService(),
            new TestCurrentUserContext(prosumer.Nic, callerRole),
            new FixedTimeProvider(CurrentTime));

        return new Fixture(prosumer, station, slot, prosumers, stations, slots, reservations, reservationService);
    }

    private sealed record Fixture(
        Prosumer Prosumer,
        SolarStation Station,
        EnergyBookingSlot Slot,
        TestProsumerRepository Prosumers,
        TestStationRepository Stations,
        TestSlotRepository Slots,
        TestReservationRepository Reservations,
        ReservationService ReservationService)
    {
        public CreateReservationRequest CreateRequest(string? prosumerId = null)
        {
            // Build an ESR request using IDs produced by the integrated component fixtures.
            return new CreateReservationRequest
            {
                ProsumerId = prosumerId ?? Prosumer.Nic,
                StationId = Station.Id,
                BookingSlotId = Slot.Id,
                ScheduledAt = CurrentTime.AddHours(2)
            };
        }
    }

    private sealed class TestCurrentUserContext : ICurrentUserContext
    {
        public TestCurrentUserContext(string userId, UserRole? role)
        {
            // Store trusted test JWT claim values.
            UserId = userId;
            Role = role;
        }

        public bool IsAuthenticated => true;
        public string? UserId { get; }
        public UserRole? Role { get; }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            // Return a deterministic UTC time for reservation-window assertions.
            return value;
        }
    }

    private sealed class TestQrTokenService : IReservationQrTokenService
    {
        public string GenerateToken()
        {
            // Return a deterministic token because QR behavior is outside this integration fixture.
            return "integration-token";
        }

        public string HashToken(string token)
        {
            // Return a deterministic hash because QR behavior is outside this integration fixture.
            return $"hash:{token}";
        }

        public bool VerifyToken(string token, string tokenHash)
        {
            // Compare deterministic values because QR behavior is outside this integration fixture.
            return HashToken(token) == tokenHash;
        }
    }

    private sealed class TestProsumerRepository(params Prosumer[] values) : IProsumerRepository
    {
        private readonly List<Prosumer> prosumers = values.ToList();

        public Task<Prosumer?> GetByNicAsync(string nic, CancellationToken cancellationToken = default)
        {
            // Resolve NIC using the same uppercase normalization enforced by the Prosumer domain entity.
            return Task.FromResult(prosumers.SingleOrDefault(prosumer => prosumer.Nic == nic.Trim().ToUpperInvariant()));
        }

        public Task<Prosumer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            // Return no email lookup because these adapter tests resolve the stable NIC only.
            return Task.FromResult<Prosumer?>(null);
        }

        public Task<PagedResult<Prosumer>> GetPagedAsync(ProsumerQuery query, CancellationToken cancellationToken = default)
        {
            // Return an unused empty page to satisfy the focused repository contract.
            return Task.FromResult(new PagedResult<Prosumer>());
        }

        public Task<bool> ExistsByNicAsync(string nic, CancellationToken cancellationToken = default)
        {
            // Return an unused existence result for this read-adapter fixture.
            return Task.FromResult(false);
        }

        public Task<bool> ExistsByEmailAsync(string email, string? excludingNic = null, CancellationToken cancellationToken = default)
        {
            // Return an unused email existence result for this read-adapter fixture.
            return Task.FromResult(false);
        }

        public Task AddAsync(Prosumer prosumer, CancellationToken cancellationToken = default)
        {
            // Ignore writes because these adapter tests start with their complete prosumer fixture.
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Prosumer prosumer, CancellationToken cancellationToken = default)
        {
            // Ignore writes because these adapter tests only read prosumer eligibility.
            return Task.CompletedTask;
        }
    }

    private sealed class TestStationRepository(params SolarStation[] values) : ISolarStationRepository
    {
        private readonly List<SolarStation> stations = values.ToList();

        public Task<SolarStation?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            // Resolve the station by its persisted string identifier.
            return Task.FromResult(stations.SingleOrDefault(station => station.Id == id.Trim()));
        }

        public Task<SolarStation?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) { /* Not used by this fixture. */ return Task.FromResult<SolarStation?>(null); }
        public Task<IReadOnlyList<SolarStation>> GetAllAsync(CancellationToken cancellationToken = default) { /* Not used by this fixture. */ return Task.FromResult<IReadOnlyList<SolarStation>>(stations); }
        public Task<PagedResult<SolarStation>> GetPagedAsync(SolarStationQuery query, CancellationToken cancellationToken = default) { /* Not used by this fixture. */ return Task.FromResult(new PagedResult<SolarStation>()); }
        public Task<IReadOnlyList<SolarStation>> GetNearbyAsync(NearbyStationQuery query, CancellationToken cancellationToken = default) { /* Not used by this fixture. */ return Task.FromResult<IReadOnlyList<SolarStation>>([]); }
        public Task<bool> ExistsByCodeAsync(string code, string? excludingStationId = null, CancellationToken cancellationToken = default) { /* Not used by this fixture. */ return Task.FromResult(false); }
        public Task AddAsync(SolarStation station, CancellationToken cancellationToken = default) { /* Not used by this fixture. */ return Task.CompletedTask; }
        public Task UpdateAsync(SolarStation station, CancellationToken cancellationToken = default) { /* Keep the mutable fixture instance in place. */ return Task.CompletedTask; }
    }

    private sealed class TestSlotRepository(params EnergyBookingSlot[] values) : IBookingSlotRepository
    {
        private readonly List<EnergyBookingSlot> slots = values.ToList();

        public Task<EnergyBookingSlot?> GetByIdAsync(string id, CancellationToken cancellationToken = default) { /* Resolve the globally persisted slot id. */ return Task.FromResult(slots.SingleOrDefault(slot => slot.Id == id.Trim())); }
        public Task<PagedResult<EnergyBookingSlot>> GetPagedAsync(BookingSlotQuery query, CancellationToken cancellationToken = default) { /* Not used by this fixture. */ return Task.FromResult(new PagedResult<EnergyBookingSlot>()); }
        public Task AddAsync(EnergyBookingSlot slot, CancellationToken cancellationToken = default) { /* Not used by this fixture. */ return Task.CompletedTask; }
        public Task UpdateAsync(EnergyBookingSlot slot, CancellationToken cancellationToken = default) { /* Keep the mutable fixture instance in place. */ return Task.CompletedTask; }
        public Task RemoveAsync(string id, CancellationToken cancellationToken = default) { /* Not used by this fixture. */ return Task.CompletedTask; }
        public Task<bool> ExistsBySlotNumberAsync(string stationId, int slotNumber, string? excludingSlotId = null, CancellationToken cancellationToken = default) { /* Not used by this fixture. */ return Task.FromResult(false); }
        public Task<bool> HasOverlappingIntervalAsync(string stationId, DateTimeOffset startTime, DateTimeOffset endTime, string? excludingSlotId = null, CancellationToken cancellationToken = default) { /* Not used by this fixture. */ return Task.FromResult(false); }
    }

    private sealed class TestReservationRepository : IReservationRepository
    {
        private readonly List<EnergyReservation> reservations = [];

        public Task<EnergyReservation?> GetByIdAsync(string id, CancellationToken cancellationToken = default) { /* Resolve a fixture reservation by id. */ return Task.FromResult(reservations.SingleOrDefault(reservation => reservation.Id == id)); }
        public Task<IReadOnlyList<EnergyReservation>> GetByProsumerIdAsync(string prosumerId, CancellationToken cancellationToken = default) { /* Return fixture reservations for one prosumer. */ return Task.FromResult<IReadOnlyList<EnergyReservation>>(reservations.Where(reservation => reservation.ProsumerId == prosumerId).ToArray()); }
        public Task<PagedResult<EnergyReservation>> GetPagedAsync(ReservationQuery query, CancellationToken cancellationToken = default) { /* Not used by this fixture. */ return Task.FromResult(new PagedResult<EnergyReservation>()); }
        public Task<PagedResult<EnergyReservation>> GetDashboardReservationsAsync(ReservationDashboardView view, ReservationQuery query, DateTimeOffset nowUtc, CancellationToken cancellationToken = default) { /* Not used by this fixture. */ return Task.FromResult(new PagedResult<EnergyReservation>()); }
        public Task<ReservationDashboardCounts> GetDashboardCountsAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default) { /* Not used by this fixture. */ return Task.FromResult(new ReservationDashboardCounts()); }
        public Task<bool> HasActiveReservationForBookingSlotAsync(string bookingSlotId, string? excludingReservationId = null, CancellationToken cancellationToken = default) { /* Detect active slot occupancy in fixture data. */ return Task.FromResult(reservations.Any(reservation => reservation.BookingSlotId == bookingSlotId && reservation.IsActive && reservation.Id != excludingReservationId)); }
        public Task<bool> HasActiveReservationsForStationAsync(string stationId, CancellationToken cancellationToken = default) { /* Detect active station reservations in fixture data. */ return Task.FromResult(reservations.Any(reservation => reservation.StationId == stationId && reservation.IsActive)); }
        public Task AddAsync(EnergyReservation reservation, CancellationToken cancellationToken = default) { /* Persist the fixture reservation for later station checks. */ reservations.Add(reservation); return Task.CompletedTask; }
        public Task UpdateAsync(EnergyReservation reservation, CancellationToken cancellationToken = default) { /* Preserve mutable fixture state in memory. */ return Task.CompletedTask; }
    }
}
