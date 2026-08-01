/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationServiceTests.cs
 * Description: Verifies energy reservation creation business rules.
 * Contributor: Dilshan Yapa S Y C T
 */

using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Application.Reservations.Requests;
using SolGrid.Application.Reservations.Services;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using Xunit;

namespace SolGrid.Application.Tests.Reservations;

public sealed class ReservationServiceTests
{
    private static readonly DateTimeOffset CurrentTime = new(2026, 9, 15, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateReservationAsync_WithValidRequest_CreatesPendingReservation()
    {
        // Verify a valid request creates and persists a pending reservation.
        var repository = new InMemoryReservationRepository();
        var service = CreateService(repository);

        var response = await service.CreateReservationAsync(CreateRequest(CurrentTime.AddHours(2)));

        var persistedReservation = await repository.GetByIdAsync(response.Id);
        Assert.NotNull(persistedReservation);
        Assert.Equal(ReservationStatus.Pending, response.Status);
        Assert.Equal(CurrentTime.AddHours(2), response.ScheduledAt);
        Assert.True(response.CreatedAt.Offset == TimeSpan.Zero);
    }

    [Fact]
    public async Task CreateReservationAsync_WithPastSchedule_ThrowsValidation()
    {
        // Verify past reservation times are rejected.
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateReservationAsync(CreateRequest(CurrentTime.AddMinutes(-1))));
    }

    [Fact]
    public async Task CreateReservationAsync_ExactlyWithinSevenDayWindow_CreatesReservation()
    {
        // Verify the assignment seven-day boundary is inclusive.
        var service = CreateService();

        var response = await service.CreateReservationAsync(CreateRequest(CurrentTime.AddDays(7)));

        Assert.Equal(CurrentTime.AddDays(7), response.ScheduledAt);
    }

    [Fact]
    public async Task CreateReservationAsync_BeyondSevenDayWindow_ThrowsValidation()
    {
        // Verify reservations after the allowed assignment window are rejected.
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateReservationAsync(CreateRequest(CurrentTime.AddDays(7).AddTicks(1))));
    }

    [Fact]
    public async Task CreateReservationAsync_WithMissingProsumer_ThrowsNotFound()
    {
        // Verify missing prosumer references are reported as not found.
        var prosumers = new FakeProsumerReadService();
        var service = CreateService(prosumerReadService: prosumers);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateReservationAsync(CreateRequest(CurrentTime.AddHours(1))));
    }

    [Fact]
    public async Task CreateReservationAsync_WithInactiveProsumer_ThrowsConflict()
    {
        // Verify inactive prosumers cannot create reservations.
        var prosumers = new FakeProsumerReadService(new ReservationProsumerSnapshot
        {
            Id = "prosumer-1",
            IsActive = false
        });
        var service = CreateService(prosumerReadService: prosumers);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateReservationAsync(CreateRequest(CurrentTime.AddHours(1))));
    }

    [Fact]
    public async Task CreateReservationAsync_WithInactiveStation_ThrowsConflict()
    {
        // Verify inactive stations cannot be reserved.
        var stations = new FakeStationReadService(new ReservationStationSnapshot
        {
            Id = "station-1",
            IsActive = false
        });
        var service = CreateService(stationReadService: stations);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateReservationAsync(CreateRequest(CurrentTime.AddHours(1))));
    }

    [Fact]
    public async Task CreateReservationAsync_WithInvalidSlot_ThrowsNotFound()
    {
        // Verify missing booking slots are reported as not found.
        var slots = new FakeBookingSlotReadService();
        var service = CreateService(bookingSlotReadService: slots);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateReservationAsync(CreateRequest(CurrentTime.AddHours(1))));
    }

    [Fact]
    public async Task CreateReservationAsync_WhenSlotBelongsToAnotherStation_ThrowsValidation()
    {
        // Verify booking slots must belong to the requested station.
        var slots = new FakeBookingSlotReadService(new ReservationBookingSlotSnapshot
        {
            Id = "slot-1",
            StationId = "station-2",
            IsActive = true,
            IsAvailable = true
        });
        var service = CreateService(bookingSlotReadService: slots);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateReservationAsync(CreateRequest(CurrentTime.AddHours(1))));
    }

    [Fact]
    public async Task CreateReservationAsync_WithUnavailableSlot_ThrowsConflict()
    {
        // Verify unavailable booking slots cannot be reserved.
        var slots = new FakeBookingSlotReadService(new ReservationBookingSlotSnapshot
        {
            Id = "slot-1",
            StationId = "station-1",
            IsActive = true,
            IsAvailable = false
        });
        var service = CreateService(bookingSlotReadService: slots);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateReservationAsync(CreateRequest(CurrentTime.AddHours(1))));
    }

    [Fact]
    public async Task CreateReservationAsync_WithDuplicateActiveReservation_ThrowsConflict()
    {
        // Verify one booking slot cannot have two active reservations.
        var existingReservation = EnergyReservation.Create(
            "reservation-existing",
            "other-prosumer",
            "station-1",
            "slot-1",
            CurrentTime.AddHours(1),
            CurrentTime);
        var repository = new InMemoryReservationRepository(existingReservation);
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateReservationAsync(CreateRequest(CurrentTime.AddHours(2))));
    }

    private static ReservationService CreateService(
        InMemoryReservationRepository? repository = null,
        FakeProsumerReadService? prosumerReadService = null,
        FakeStationReadService? stationReadService = null,
        FakeBookingSlotReadService? bookingSlotReadService = null)
    {
        // Create a reservation service with deterministic test dependencies.
        return new ReservationService(
            repository ?? new InMemoryReservationRepository(),
            prosumerReadService ?? new FakeProsumerReadService(new ReservationProsumerSnapshot
            {
                Id = "prosumer-1",
                IsActive = true
            }),
            stationReadService ?? new FakeStationReadService(new ReservationStationSnapshot
            {
                Id = "station-1",
                IsActive = true
            }),
            bookingSlotReadService ?? new FakeBookingSlotReadService(new ReservationBookingSlotSnapshot
            {
                Id = "slot-1",
                StationId = "station-1",
                IsActive = true,
                IsAvailable = true
            }),
            new FixedTimeProvider(CurrentTime));
    }

    private static CreateReservationRequest CreateRequest(DateTimeOffset scheduledAt)
    {
        // Create a valid reservation request for service tests.
        return new CreateReservationRequest
        {
            ProsumerId = "prosumer-1",
            StationId = "station-1",
            BookingSlotId = "slot-1",
            ScheduledAt = scheduledAt
        };
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset currentTime;

        public FixedTimeProvider(DateTimeOffset currentTime)
        {
            // Store the deterministic test clock value.
            this.currentTime = currentTime;
        }

        public override DateTimeOffset GetUtcNow()
        {
            // Return the deterministic UTC time for reservation rule tests.
            return currentTime;
        }
    }

    private sealed class FakeProsumerReadService : IReservationProsumerReadService
    {
        private readonly ReservationProsumerSnapshot? prosumer;

        public FakeProsumerReadService(ReservationProsumerSnapshot? prosumer = null)
        {
            // Store the optional prosumer snapshot for tests.
            this.prosumer = prosumer;
        }

        public Task<ReservationProsumerSnapshot?> GetByIdAsync(
            string prosumerId,
            CancellationToken cancellationToken = default)
        {
            // Return the configured prosumer when ids match.
            return Task.FromResult(prosumer?.Id == prosumerId ? prosumer : null);
        }
    }

    private sealed class FakeStationReadService : IReservationStationReadService
    {
        private readonly ReservationStationSnapshot? station;

        public FakeStationReadService(ReservationStationSnapshot? station = null)
        {
            // Store the optional station snapshot for tests.
            this.station = station;
        }

        public Task<ReservationStationSnapshot?> GetByIdAsync(
            string stationId,
            CancellationToken cancellationToken = default)
        {
            // Return the configured station when ids match.
            return Task.FromResult(station?.Id == stationId ? station : null);
        }
    }

    private sealed class FakeBookingSlotReadService : IReservationBookingSlotReadService
    {
        private readonly ReservationBookingSlotSnapshot? bookingSlot;

        public FakeBookingSlotReadService(ReservationBookingSlotSnapshot? bookingSlot = null)
        {
            // Store the optional booking slot snapshot for tests.
            this.bookingSlot = bookingSlot;
        }

        public Task<ReservationBookingSlotSnapshot?> GetByIdAsync(
            string bookingSlotId,
            CancellationToken cancellationToken = default)
        {
            // Return the configured booking slot when ids match.
            return Task.FromResult(bookingSlot?.Id == bookingSlotId ? bookingSlot : null);
        }
    }

    private sealed class InMemoryReservationRepository : IReservationRepository
    {
        private readonly List<EnergyReservation> reservations;

        public InMemoryReservationRepository(params EnergyReservation[] reservations)
        {
            // Store test reservations in memory.
            this.reservations = reservations.ToList();
        }

        public Task<EnergyReservation?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            // Find a reservation by id in memory.
            return Task.FromResult(reservations.FirstOrDefault(reservation => reservation.Id == id));
        }

        public Task<IReadOnlyList<EnergyReservation>> GetByProsumerIdAsync(
            string prosumerId,
            CancellationToken cancellationToken = default)
        {
            // Return reservations for one prosumer in memory.
            return Task.FromResult<IReadOnlyList<EnergyReservation>>(
                reservations.Where(reservation => reservation.ProsumerId == prosumerId).ToArray());
        }

        public Task<PagedResult<EnergyReservation>> GetPagedAsync(
            ReservationQuery query,
            CancellationToken cancellationToken = default)
        {
            // Return all in-memory reservations in a single page.
            return Task.FromResult(new PagedResult<EnergyReservation>
            {
                Items = reservations,
                TotalCount = reservations.Count,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            });
        }

        public Task<bool> HasActiveReservationForBookingSlotAsync(
            string bookingSlotId,
            string? excludingReservationId = null,
            CancellationToken cancellationToken = default)
        {
            // Check for active reservations assigned to the same booking slot.
            return Task.FromResult(reservations.Any(reservation =>
                reservation.BookingSlotId == bookingSlotId
                && reservation.IsActive
                && reservation.Id != excludingReservationId));
        }

        public Task<bool> HasActiveReservationsForStationAsync(
            string stationId,
            CancellationToken cancellationToken = default)
        {
            // Check for active reservations assigned to the same station.
            return Task.FromResult(reservations.Any(reservation =>
                reservation.StationId == stationId && reservation.IsActive));
        }

        public Task AddAsync(EnergyReservation reservation, CancellationToken cancellationToken = default)
        {
            // Add a reservation to the in-memory store.
            reservations.Add(reservation);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(EnergyReservation reservation, CancellationToken cancellationToken = default)
        {
            // Updates are not required for create reservation tests.
            return Task.CompletedTask;
        }
    }
}
