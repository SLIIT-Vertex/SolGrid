/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: MongoProsumerRepositoryTests.cs
 * Description: Verifies MongoDB prosumer persistence and uniqueness behavior.
 * Contributor: Gunasekara H N
 */

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;
using SolGrid.Domain.Enums;
using SolGrid.Application.Common.Exceptions;
using SolGrid.Domain.Entities;
using SolGrid.Infrastructure.Persistence.MongoDb;
using SolGrid.Infrastructure.Persistence.MongoDb.Repositories;
using Xunit;

namespace SolGrid.Infrastructure.Tests.Repositories;

public sealed class MongoProsumerRepositoryTests : IAsyncLifetime
{
    private readonly string? connectionString = Environment.GetEnvironmentVariable("SOLGRID_MONGO_TEST_CONNECTION_STRING");
    private readonly string databaseName = $"SG_Prosumer_{Guid.NewGuid():N}";
    private IMongoClient? mongoClient;
    private MongoProsumerRepository? repository;

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
        var initializer = new MongoProsumerCollectionInitializer(database, options);
        await initializer.EnsureCreatedAsync();
        repository = new MongoProsumerRepository(database, options);
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
    public async Task AddAndGetByNicAsync_PersistsProsumer()
    {
        // Verify a prosumer can be inserted and restored with its hashed credential value.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var prosumer = CreateProsumer("199012345678", "prosumer@example.com");

        await repository!.AddAsync(prosumer);
        var persistedProsumer = await repository.GetByNicAsync(prosumer.Nic);

        Assert.NotNull(persistedProsumer);
        Assert.Equal(prosumer.Email, persistedProsumer.Email);
        Assert.Equal("hashed-password", persistedProsumer.PasswordHash);
    }

    [Fact]
    public async Task AddAsync_RejectsDuplicateNicAndEmail()
    {
        // Verify MongoDB enforces NIC primary-key and email unique-index rules.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        await repository!.AddAsync(CreateProsumer("199012345678", "first@example.com"));

        await Assert.ThrowsAsync<ConflictException>(() => repository.AddAsync(CreateProsumer("199012345678", "second@example.com")));
        await Assert.ThrowsAsync<ConflictException>(() => repository.AddAsync(CreateProsumer("199012345679", "first@example.com")));
    }

    [Fact]
    public async Task ConcurrentUpdates_KeepWinningStatusAndHistoryTogether()
    {
        // Both writers load the same version; only one status/profile replacement can succeed.
        if (ShouldSkipWithoutMongo()) return;
        var initial = CreateProsumer("199012345678", "prosumer@example.com");
        await repository!.AddAsync(initial);
        var first = (await repository.GetByNicAsync(initial.Nic))!;
        var stale = (await repository.GetByNicAsync(initial.Nic))!;
        first.Deactivate(DateTimeOffset.UtcNow);
        first.RecordActivity("Deactivated", "reviewer", "Backoffice", "Reviewed", DateTimeOffset.UtcNow);
        stale.UpdateProfile("Stale", "Writer", initial.Email, null, DateTimeOffset.UtcNow);
        stale.RecordActivity("ProfileUpdated", initial.Nic, "Prosumer", null, DateTimeOffset.UtcNow);
        await repository.UpdateAsync(first);
        await Assert.ThrowsAsync<ConflictException>(() => repository.UpdateAsync(stale));
        var persisted = (await repository.GetByNicAsync(initial.Nic))!;
        Assert.Equal(ProsumerAccountStatus.Deactivated, persisted.Status);
        Assert.Equal(1, persisted.Version);
        Assert.Equal("Deactivated", Assert.Single(persisted.Activity).Action);
        Assert.Equal(initial.FirstName, persisted.FirstName);
    }

    [Fact]
    public async Task LegacyDocumentWithoutVersionOrHistory_UpgradesOnFirstWrite()
    {
        // Existing production documents need no destructive migration or fabricated audit history.
        if (ShouldSkipWithoutMongo()) return;
        var initial = CreateProsumer("199012345678", "prosumer@example.com");
        await repository!.AddAsync(initial);
        var collection = mongoClient!.GetDatabase(databaseName).GetCollection<BsonDocument>("Prosumers");
        await collection.UpdateOneAsync(new BsonDocument("_id", initial.Nic),
            Builders<BsonDocument>.Update.Unset("Version").Unset("Activity"));
        var loaded = (await repository.GetByNicAsync(initial.Nic))!;
        Assert.Equal(0, loaded.Version);
        Assert.Empty(loaded.Activity);
        loaded.Activate(DateTimeOffset.UtcNow);
        loaded.RecordActivity("Activated", "reviewer", "Backoffice", "Reviewed", DateTimeOffset.UtcNow);
        await repository.UpdateAsync(loaded);
        var persisted = (await repository.GetByNicAsync(initial.Nic))!;
        Assert.Equal(1, persisted.Version);
        Assert.Single(persisted.Activity);
    }

    private static Prosumer CreateProsumer(string nic, string email)
    {
        // Create a valid domain prosumer for repository integration tests.
        return Prosumer.Create(
            nic,
            "Test",
            "Prosumer",
            email,
            null,
            "hashed-password",
            DateTimeOffset.UtcNow);
    }

    private bool ShouldSkipWithoutMongo()
    {
        // Bypass integration work when no MongoDB connection is configured for the test run.
        return string.IsNullOrWhiteSpace(connectionString);
    }
}
