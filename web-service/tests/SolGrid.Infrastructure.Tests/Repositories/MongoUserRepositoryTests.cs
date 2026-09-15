/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: MongoUserRepositoryTests.cs
 * Description: Verifies MongoDB user repository behavior for persistence operations.
 * Contributor: Bawanthi K D R
 */

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using SolGrid.Infrastructure.Persistence.MongoDb;
using SolGrid.Infrastructure.Persistence.MongoDb.Repositories;
using Xunit;

namespace SolGrid.Infrastructure.Tests.Repositories;

public sealed class MongoUserRepositoryTests : IAsyncLifetime
{
    private readonly string? connectionString = Environment.GetEnvironmentVariable("SOLGRID_MONGO_TEST_CONNECTION_STRING");
    private readonly string databaseName = $"SolGrid_UserRepositoryTests_{Guid.NewGuid():N}";
    private IMongoClient? mongoClient;
    private MongoUserRepository? repository;

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
        var initializer = new MongoUserCollectionInitializer(database, options);
        await initializer.EnsureCreatedAsync();
        repository = new MongoUserRepository(database, options);
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
    public async Task AddAndGetByIdAsync_PersistsUser()
    {
        // Verify that a user can be inserted and fetched by id.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var user = CreateUser("crud@example.com");

        await repository!.AddAsync(user);
        var persistedUser = await repository.GetByIdAsync(user.Id);

        Assert.NotNull(persistedUser);
        Assert.Equal(user.Email, persistedUser.Email);
        Assert.Equal(UserRole.Backoffice, persistedUser.Role);
        Assert.Equal(AccountStatus.Active, persistedUser.Status);
    }

    [Fact]
    public async Task AddAsync_RejectsDuplicateEmail()
    {
        // Verify that MongoDB enforces unique email persistence.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var firstUser = CreateUser("duplicate@example.com");
        var secondUser = CreateUser("duplicate@example.com", UserRole.GridOperator);

        await repository!.AddAsync(firstUser);

        await Assert.ThrowsAsync<MongoWriteException>(() => repository.AddAsync(secondUser));
    }

    [Fact]
    public async Task UpdateAsync_ReplacesExistingUser()
    {
        // Verify that updating a domain user replaces the persisted document.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var user = CreateUser("update@example.com");

        await repository!.AddAsync(user);
        user.Deactivate(DateTimeOffset.UtcNow);
        await repository.UpdateAsync(user);
        var persistedUser = await repository.GetByIdAsync(user.Id);

        Assert.NotNull(persistedUser);
        Assert.Equal(AccountStatus.Inactive, persistedUser.Status);
    }

    [Fact]
    public async Task ExistsByEmailAsync_ExcludesCurrentUserWhenRequested()
    {
        // Verify email existence checks support update scenarios.
        if (ShouldSkipWithoutMongo())
        {
            return;
        }

        var user = CreateUser("exists@example.com");

        await repository!.AddAsync(user);

        Assert.True(await repository.ExistsByEmailAsync(user.Email));
        Assert.False(await repository.ExistsByEmailAsync(user.Email, user.Id));
    }

    private static User CreateUser(string email, UserRole role = UserRole.Backoffice)
    {
        // Create a valid domain user for repository integration tests.
        return User.Create(
            Guid.NewGuid().ToString("N"),
            "Test",
            "User",
            email,
            "hashed-password",
            role,
            DateTimeOffset.UtcNow);
    }

    private bool ShouldSkipWithoutMongo()
    {
        // Bypass integration work when no MongoDB connection is configured for the test run.
        return string.IsNullOrWhiteSpace(connectionString);
    }
}
