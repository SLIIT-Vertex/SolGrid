/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: InfrastructureDependencyInjection.cs
 * Description: Registers SolGrid infrastructure persistence dependencies.
 * Contributor: Bawanthi K D R
 */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Infrastructure.Persistence.MongoDb;
using SolGrid.Infrastructure.Persistence.MongoDb.Repositories;
using SolGrid.Infrastructure.Persistence.MongoDb.Seeding;

namespace SolGrid.Infrastructure.DependencyInjection;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddSolGridInfrastructure(
        this IServiceCollection services,
        Action<MongoDbOptions> configureMongoDb)
    {
        // Register MongoDB persistence services for SolGrid infrastructure.
        services.Configure(configureMongoDb);
        services.AddSingleton<IMongoClient>(serviceProvider =>
        {
            // Create the MongoDB client from configured connection settings.
            var options = serviceProvider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            return new MongoClient(options.ConnectionString);
        });
        services.AddSingleton(serviceProvider =>
        {
            // Resolve the configured MongoDB database for repository implementations.
            var options = serviceProvider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            var mongoClient = serviceProvider.GetRequiredService<IMongoClient>();
            return mongoClient.GetDatabase(options.DatabaseName);
        });
        services.AddScoped<IUserRepository, MongoUserRepository>();
        services.AddScoped<IUserCollectionInitializer, MongoUserCollectionInitializer>();
        services.AddScoped<IUserSeedDataInitializer, MongoUserSeedDataInitializer>();

        return services;
    }
}
