/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: MongoBookingSlotRepositoryTests.cs
 * Description: Verifies MongoDB EnergyBookingSlots persistence, uniqueness, and indexes.
 * Contributor: Kavishi Godage
 */

using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.SolarStations.Interfaces;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using SolGrid.Infrastructure.Persistence.MongoDb;
using SolGrid.Infrastructure.Persistence.MongoDb.Repositories;
using Xunit;

namespace SolGrid.Infrastructure.Tests.Repositories;

public sealed class MongoBookingSlotRepositoryTests : IAsyncLifetime
{
    private readonly string? connectionString = Environment.GetEnvironmentVariable("SOLGRID_MONGO_TEST_CONNECTION_STRING");
    private readonly string databaseName = $"SolGrid_BookingSlotRepositoryTests_{Guid.NewGuid():N}";
    private IMongoClient? mongoClient;
    private IMongoDatabase? database;
    private MongoDbOptions? options;
    private MongoBookingSlotRepository? repository;

    public async Task InitializeAsync()
    {
        // Prepare a temporary MongoDB database when an integration test connection is configured.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        options = new MongoDbOptions
        {
            ConnectionString = connectionString,
            DatabaseName = databaseName,
            EnergyBookingSlotsCollectionName = "EnergyBookingSlots"
        };
        var optionsWrapper = Options.Create(options);

        mongoClient = new MongoClient(connectionString);
        database = mongoClient.GetDatabase(databaseName);
        var initializer = new MongoBookingSlotCollectionInitializer(database, optionsWrapper);
        await initializer.EnsureCreatedAsync();
        repository = new MongoBookingSlotRepository(database, optionsWrapper);
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
    public async Task EnsureCreatedAsync_CreatesRequiredIndexes()
    {
        // Verify EnergyBookingSlots exposes uniqueness, station, time, and status indexes.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var collection = database!.GetCollection<BsonDocument>(options!.EnergyBookingSlotsCollectionName);
        var indexes = await (await collection.Indexes.ListAsync()).ToListAsync();
        var indexNames = indexes.Select(index => index["name"].AsString).ToArray();
        var uniqueSlotNumberIndex = Assert.Single(
            indexes, index => index["name"].AsString == "ux_energy_booking_slots_station_slot_number");

        Assert.Contains("ux_energy_booking_slots_station_slot_number", indexNames);
        Assert.Contains("ix_energy_booking_slots_station_id", indexNames);
        Assert.Contains("ix_energy_booking_slots_start_time", indexNames);
        Assert.Contains("ix_energy_booking_slots_status", indexNames);
        Assert.Contains("ix_energy_booking_slots_station_start_end", indexNames);
        Assert.True(uniqueSlotNumberIndex.GetValue("unique", false).ToBoolean());
    }

    [Fact]
    public async Task AddAndGetByIdAsync_PersistsSlot()
    {
        // Verify that a slot can be inserted and fetched by id.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var slot = CreateSlot("station-1", 1);

        await repository!.AddAsync(slot);
        var persistedSlot = await repository.GetByIdAsync(slot.Id);

        Assert.NotNull(persistedSlot);
        Assert.Equal(slot.StationId, persistedSlot.StationId);
        Assert.Equal(slot.SlotNumber, persistedSlot.SlotNumber);
        Assert.Equal(slot.BatteryCapacityKwh, persistedSlot.BatteryCapacityKwh);
        Assert.Equal(slot.StartTime, persistedSlot.StartTime);
        Assert.Equal(slot.EndTime, persistedSlot.EndTime);
        Assert.Equal(SlotStatus.Available, persistedSlot.Status);
    }

    [Fact]
    public async Task AddAsync_RejectsDuplicateSlotNumberForStation()
    {
        // Verify that MongoDB enforces unique slot numbers per station.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var firstSlot = CreateSlot("station-1", 1);
        var secondSlot = CreateSlot("station-1", 1);

        await repository!.AddAsync(firstSlot);

        await Assert.ThrowsAsync<ConflictException>(() => repository.AddAsync(secondSlot));
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByStationStatusAndOverlappingInterval()
    {
        // Verify station, status, and overlapping time filters are applied by EnergyBookingSlots queries.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var matchingSlot = CreateSlot("station-1", 1, new DateTimeOffset(2026, 9, 14, 8, 0, 0, TimeSpan.Zero));
        matchingSlot.TakeOutOfService(DateTimeOffset.UtcNow);
        var overlappingAvailable = CreateSlot("station-1", 2, new DateTimeOffset(2026, 9, 14, 9, 0, 0, TimeSpan.Zero));
        var otherStationSlot = CreateSlot("station-2", 1, new DateTimeOffset(2026, 9, 14, 8, 0, 0, TimeSpan.Zero));

        await repository!.AddAsync(matchingSlot);
        await repository.AddAsync(overlappingAvailable);
        await repository.AddAsync(otherStationSlot);

        var page = await repository.GetPagedAsync(new BookingSlotQuery
        {
            StationId = "station-1",
            Status = SlotStatus.OutOfService,
            From = new DateTimeOffset(2026, 9, 14, 8, 30, 0, TimeSpan.Zero),
            To = new DateTimeOffset(2026, 9, 14, 10, 0, 0, TimeSpan.Zero),
            PageNumber = 1,
            PageSize = 10
        });

        var slot = Assert.Single(page.Items);
        Assert.Equal(matchingSlot.Id, slot.Id);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task HasOverlappingIntervalAsync_DetectsOverlappingStationSlots()
    {
        // Verify overlapping interval detection used by duplicate/overlap rules.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var existingSlot = CreateSlot("station-1", 1, new DateTimeOffset(2026, 9, 14, 8, 0, 0, TimeSpan.Zero));
        await repository!.AddAsync(existingSlot);

        Assert.True(await repository.HasOverlappingIntervalAsync(
            "station-1",
            new DateTimeOffset(2026, 9, 14, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 14, 11, 0, 0, TimeSpan.Zero)));
        Assert.False(await repository.HasOverlappingIntervalAsync(
            "station-1",
            new DateTimeOffset(2026, 9, 14, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 14, 12, 0, 0, TimeSpan.Zero)));
        Assert.False(await repository.HasOverlappingIntervalAsync(
            "station-1",
            new DateTimeOffset(2026, 9, 14, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 14, 11, 0, 0, TimeSpan.Zero),
            existingSlot.Id));
    }

    [Fact]
    public async Task UpdateAsync_ReplacesExistingSlot()
    {
        // Verify that updating a domain slot replaces the persisted document.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var slot = CreateSlot("station-1", 1);

        await repository!.AddAsync(slot);
        slot.TakeOutOfService(DateTimeOffset.UtcNow);
        await repository.UpdateAsync(slot);
        var persistedSlot = await repository.GetByIdAsync(slot.Id);

        Assert.NotNull(persistedSlot);
        Assert.Equal(SlotStatus.OutOfService, persistedSlot.Status);
        Assert.False(persistedSlot.IsActive);
    }

    [Fact]
    public async Task ExistsBySlotNumberAsync_ExcludesCurrentSlotWhenRequested()
    {
        // Verify slot-number existence checks support update scenarios.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var slot = CreateSlot("station-1", 4);

        await repository!.AddAsync(slot);

        Assert.True(await repository.ExistsBySlotNumberAsync(slot.StationId, slot.SlotNumber));
        Assert.False(await repository.ExistsBySlotNumberAsync(slot.StationId, slot.SlotNumber, slot.Id));
        Assert.False(await repository.ExistsBySlotNumberAsync("station-2", slot.SlotNumber));
    }

    private static EnergyBookingSlot CreateSlot(string stationId, int slotNumber, DateTimeOffset? startTime = null)
    {
        // Create a valid domain slot for repository integration tests.
        var start = startTime ?? new DateTimeOffset(2026, 9, 14, 8, 0, 0, TimeSpan.Zero);
        return EnergyBookingSlot.Create(
            Guid.NewGuid().ToString("N"),
            stationId,
            slotNumber,
            12.5m,
            start,
            start.AddHours(2),
            DateTimeOffset.UtcNow);
    }

    private bool ShouldSkipWithoutMongo()
    {
        // Bypass integration work when no MongoDB connection is configured for the test run.
        return string.IsNullOrWhiteSpace(connectionString);
    }
}
