/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: MongoUserSeedDataInitializer.cs
 * Description: Seeds optional development web user accounts into MongoDB.
 * Contributor: Bawanthi K D R
 */

using Microsoft.Extensions.Options;
using MongoDB.Bson;
using SolGrid.Application.Auth.Interfaces;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;

namespace SolGrid.Infrastructure.Persistence.MongoDb.Seeding;

internal sealed class MongoUserSeedDataInitializer : IUserSeedDataInitializer
{
    private readonly MongoDbOptions options;
    private readonly IUserRepository userRepository;
    private readonly IPasswordHasher passwordHasher;

    public MongoUserSeedDataInitializer(
        IOptions<MongoDbOptions> options,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        // Capture dependencies required to create optional development users.
        this.options = options.Value;
        this.userRepository = userRepository;
        this.passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Seed configured development accounts only when explicitly enabled.
        if (!options.SeedDevelopmentUsers)
        {
            return;
        }

        await SeedUserAsync(options.BackofficeSeedUser, UserRole.Backoffice, cancellationToken).ConfigureAwait(false);
        await SeedUserAsync(options.GridOperatorSeedUser, UserRole.GridOperator, cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedUserAsync(
        SeedUserOptions? seedUser,
        UserRole role,
        CancellationToken cancellationToken)
    {
        // Create one configured seed user when its email and password are supplied.
        if (seedUser is null ||
            string.IsNullOrWhiteSpace(seedUser.Email) ||
            string.IsNullOrWhiteSpace(seedUser.Password))
        {
            return;
        }

        if (await userRepository.ExistsByEmailAsync(seedUser.Email, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var user = User.Create(
            ObjectId.GenerateNewId().ToString(),
            seedUser.FirstName,
            seedUser.LastName,
            seedUser.Email,
            passwordHasher.HashPassword(seedUser.Password),
            role,
            now);

        await userRepository.AddAsync(user, cancellationToken).ConfigureAwait(false);
    }
}
