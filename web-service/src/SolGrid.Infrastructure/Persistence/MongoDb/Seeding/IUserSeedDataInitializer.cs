/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IUserSeedDataInitializer.cs
 * Description: Defines development user seed initialization for MongoDB persistence.
 * Contributor: Bawanthi K D R
 */

namespace SolGrid.Infrastructure.Persistence.MongoDb.Seeding;

public interface IUserSeedDataInitializer
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
