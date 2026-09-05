/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: MongoSolarStationRepositoryTests.cs
 * Description: Verifies MongoDB SolarStationInfo persistence, uniqueness, and indexes.
 * Contributor: Kavishi Godage
 */

using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.SolarStations.Interfaces;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using SolGrid.Domain.ValueObjects;
using SolGrid.Infrastructure.Persistence.MongoDb;
using SolGrid.Infrastructure.Persistence.MongoDb.Repositories;
using Xunit;

namespace SolGrid.Infrastructure.Tests.Repositories;

public sealed class MongoSolarStationRepositoryTests : IAsyncLifetime
{
    private readonly string? connectionString = Environment.GetEnvironmentVariable("SOLGRID_MONGO_TEST_CONNECTION_STRING");
    private readonly string databaseName = $"SolGrid_StationRepositoryTests_{Guid.NewGuid():N}";
    private IMongoClient? mongoClient;
    private IMongoDatabase? database;
    private MongoDbOptions? options;
    private MongoSolarStationRepository? repository;

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
            SolarStationInfoCollectionName = "SolarStationInfo"
        };
        var optionsWrapper = Options.Create(options);

        mongoClient = new MongoClient(connectionString);
        database = mongoClient.GetDatabase(databaseName);
        var initializer = new MongoSolarStationCollectionInitializer(database, optionsWrapper);
        await initializer.EnsureCreatedAsync();
        repository = new MongoSolarStationRepository(database, optionsWrapper);
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
        // Verify SolarStationInfo exposes unique code, status, and geo indexes.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var collection = database!.GetCollection<BsonDocument>(options!.SolarStationInfoCollectionName);
        var indexes = await (await collection.Indexes.ListAsync()).ToListAsync();
        var indexNames = indexes.Select(index => index["name"].AsString).ToArray();
        var uniqueCodeIndex = Assert.Single(indexes, index => index["name"].AsString == "ux_solar_station_info_code");
        var geoIndex = Assert.Single(indexes, index => index["name"].AsString == "ix_solar_station_info_geo_location");

        Assert.Contains("ux_solar_station_info_code", indexNames);
        Assert.Contains("ix_solar_station_info_status", indexNames);
        Assert.Contains("ix_solar_station_info_geo_location", indexNames);
        Assert.True(uniqueCodeIndex.GetValue("unique", false).ToBoolean());
        Assert.Equal("2dsphere", geoIndex["key"]["GeoLocation"].AsString);
    }

    [Fact]
    public async Task AddAndGetByIdAsync_PersistsStation()
    {
        // Verify that a station can be inserted and fetched by id.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var station = CreateStation("ST-CRUD");

        await repository!.AddAsync(station);
        var persistedStation = await repository.GetByIdAsync(station.Id);

        Assert.NotNull(persistedStation);
        Assert.Equal(station.Code, persistedStation.Code);
        Assert.Equal(station.Location.Latitude, persistedStation.Location.Latitude);
        Assert.Equal(station.Location.Longitude, persistedStation.Location.Longitude);
        Assert.Equal(StationStatus.Active, persistedStation.Status);
        Assert.Equal(station.Schedule[0].OpensAt, persistedStation.Schedule[0].OpensAt);
    }

    [Fact]
    public async Task AddAsync_RejectsDuplicateStationCode()
    {
        // Verify that MongoDB enforces unique station code persistence.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var firstStation = CreateStation("ST-DUP");
        var secondStation = CreateStation("ST-DUP");

        await repository!.AddAsync(firstStation);

        await Assert.ThrowsAsync<ConflictException>(() => repository.AddAsync(secondStation));
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByStatusAndSearch()
    {
        // Verify status and search filters are applied by SolarStationInfo queries.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var matchingStation = CreateStation("ST-NORTH", "Colombo North Hub");
        matchingStation.Deactivate(DateTimeOffset.UtcNow);
        var otherStation = CreateStation("ST-SOUTH", "Galle South Hub");

        await repository!.AddAsync(matchingStation);
        await repository.AddAsync(otherStation);

        var page = await repository.GetPagedAsync(new SolarStationQuery
        {
            SearchText = "north",
            Status = StationStatus.Inactive,
            PageNumber = 1,
            PageSize = 10
        });

        var station = Assert.Single(page.Items);
        Assert.Equal("ST-NORTH", station.Code);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesExistingStation()
    {
        // Verify that updating a domain station replaces the persisted document.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var station = CreateStation("ST-UPD");

        await repository!.AddAsync(station);
        station.Deactivate(DateTimeOffset.UtcNow);
        await repository.UpdateAsync(station);
        var persistedStation = await repository.GetByIdAsync(station.Id);

        Assert.NotNull(persistedStation);
        Assert.Equal(StationStatus.Inactive, persistedStation.Status);
    }

    [Fact]
    public async Task ExistsByCodeAsync_ExcludesCurrentStationWhenRequested()
    {
        // Verify code existence checks support update scenarios.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var station = CreateStation("ST-EXISTS");

        await repository!.AddAsync(station);

        Assert.True(await repository.ExistsByCodeAsync(station.Code));
        Assert.False(await repository.ExistsByCodeAsync(station.Code, station.Id));
    }

    private static SolarStation CreateStation(string code, string name = "Station")
    {
        // Create a valid domain station for repository integration tests.
        return SolarStation.Create(
            Guid.NewGuid().ToString("N"),
            code,
            name,
            "Colombo",
            GeoCoordinates.Create(6.9271, 79.8612),
            50m,
            [],
            [OperatingWindow.Create(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0))],
            DateTimeOffset.UtcNow);
    }

    private bool ShouldSkipWithoutMongo()
    {
        // Bypass integration work when no MongoDB connection is configured for the test run.
        return string.IsNullOrWhiteSpace(connectionString);
    }
}
