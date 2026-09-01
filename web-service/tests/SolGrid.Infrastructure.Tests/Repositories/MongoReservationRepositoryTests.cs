/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: MongoReservationRepositoryTests.cs
 * Description: Verifies MongoDB reservation repository behavior for persistence operations.
 * Contributor: Dilshan Yapa S Y C T
 */

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using SolGrid.Infrastructure.Persistence.MongoDb;
using SolGrid.Infrastructure.Persistence.MongoDb.Repositories;
using Xunit;

namespace SolGrid.Infrastructure.Tests.Repositories;

public sealed class MongoReservationRepositoryTests : IAsyncLifetime
{
    private readonly string? connectionString = Environment.GetEnvironmentVariable("SOLGRID_MONGO_TEST_CONNECTION_STRING");
    private readonly string databaseName = $"SolGrid_ReservationRepositoryTests_{Guid.NewGuid():N}";
    private IMongoClient? mongoClient;
    private MongoReservationRepository? repository;

    public async Task InitializeAsync()
    {
        // Prepare a temporary MongoDB database when an integration test connection is configured.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var options = Options.Create(new MongoDbOptions
        {
            ConnectionString = connectionString,
            DatabaseName = databaseName
        });

        mongoClient = new MongoClient(connectionString);
        var database = mongoClient.GetDatabase(databaseName);
        var initializer = new MongoReservationCollectionInitializer(database, options);
        await initializer.EnsureCreatedAsync();
        repository = new MongoReservationRepository(database, options);
    }

    public async Task DisposeAsync()
    {
        // Remove the temporary MongoDB database created for integration tests.
        if (mongoClient is not null)
        {
            await mongoClient.DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task AddAndGetByIdAsync_PersistsReservation()
    {
        // Verify that a reservation can be inserted and fetched by id.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var reservation = CreateReservation("reservation-crud", "prosumer-1", "station-1", "slot-1");

        await repository!.AddAsync(reservation);
        var persistedReservation = await repository.GetByIdAsync(reservation.Id);

        Assert.NotNull(persistedReservation);
        Assert.Equal(reservation.ProsumerId, persistedReservation.ProsumerId);
        Assert.Equal(ReservationStatus.Pending, persistedReservation.Status);
        Assert.True(persistedReservation.ScheduledAt.Offset == TimeSpan.Zero);
    }

    [Fact]
    public async Task GetByProsumerIdAsync_ReturnsOnlyProsumerReservations()
    {
        // Verify prosumer filtering returns matching reservations only.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        await repository!.AddAsync(CreateReservation("reservation-first", "prosumer-1", "station-1", "slot-1"));
        await repository.AddAsync(CreateReservation("reservation-second", "prosumer-2", "station-1", "slot-2"));

        var reservations = await repository.GetByProsumerIdAsync("prosumer-1");

        var reservation = Assert.Single(reservations);
        Assert.Equal("reservation-first", reservation.Id);
    }

    [Fact]
    public async Task GetPagedAsync_AppliesFiltersAndPaging()
    {
        // Verify paged queries support key reservation filters.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        await repository!.AddAsync(CreateReservation("reservation-first", "prosumer-1", "station-1", "slot-1"));
        await repository.AddAsync(CreateReservation("reservation-second", "prosumer-1", "station-2", "slot-2"));

        var page = await repository.GetPagedAsync(new ReservationQuery
        {
            ProsumerId = "prosumer-1",
            StationId = "station-1",
            Status = ReservationStatus.Pending,
            PageNumber = 1,
            PageSize = 10
        });

        var reservation = Assert.Single(page.Items);
        Assert.Equal("reservation-first", reservation.Id);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task HasActiveReservationForBookingSlotAsync_DetectsActiveReservation()
    {
        // Verify active slot conflict detection ignores inactive reservation statuses.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var activeReservation = CreateReservation("reservation-active", "prosumer-1", "station-1", "slot-active");
        var cancelledReservation = CreateReservation("reservation-cancelled", "prosumer-1", "station-1", "slot-cancelled");
        cancelledReservation.Cancel(DateTimeOffset.UtcNow);

        await repository!.AddAsync(activeReservation);
        await repository.AddAsync(cancelledReservation);

        Assert.True(await repository.HasActiveReservationForBookingSlotAsync("slot-active"));
        Assert.False(await repository.HasActiveReservationForBookingSlotAsync("slot-cancelled"));
        Assert.False(await repository.HasActiveReservationForBookingSlotAsync("slot-active", activeReservation.Id));
    }

    [Fact]
    public async Task UpdateAsync_ReplacesExistingReservation()
    {
        // Verify that updating a domain reservation replaces the persisted document.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var reservation = CreateReservation("reservation-update", "prosumer-1", "station-1", "slot-1");

        await repository!.AddAsync(reservation);
        reservation.Cancel(DateTimeOffset.UtcNow);
        await repository.UpdateAsync(reservation);
        var persistedReservation = await repository.GetByIdAsync(reservation.Id);

        Assert.NotNull(persistedReservation);
        Assert.Equal(ReservationStatus.Cancelled, persistedReservation.Status);
    }

    private static EnergyReservation CreateReservation(
        string id,
        string prosumerId,
        string stationId,
        string bookingSlotId)
    {
        // Create a valid domain reservation for repository integration tests.
        var now = DateTimeOffset.UtcNow;
        return EnergyReservation.Create(
            id,
            prosumerId,
            stationId,
            bookingSlotId,
            now.AddHours(1),
            now);
    }

    private bool ShouldSkipWithoutMongo()
    {
        // Bypass integration work when no MongoDB connection is configured for the test run.
        return string.IsNullOrWhiteSpace(connectionString);
    }
}
