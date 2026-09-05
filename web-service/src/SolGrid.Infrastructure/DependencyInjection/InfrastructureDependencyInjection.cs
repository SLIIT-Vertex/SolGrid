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
using SolGrid.Application.Auth.Interfaces;
using SolGrid.Application.Prosumers.Interfaces;
using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Application.SolarStations.Interfaces;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Infrastructure.Persistence.MongoDb;
using SolGrid.Infrastructure.Persistence.MongoDb.Repositories;
using SolGrid.Infrastructure.Persistence.MongoDb.Seeding;
using SolGrid.Infrastructure.Security;

namespace SolGrid.Infrastructure.DependencyInjection;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddSolGridInfrastructure(
        this IServiceCollection services,
        Action<MongoDbOptions> configureMongoDb,
        Action<JwtOptions> configureJwt)
    {
        // Register MongoDB persistence services for SolGrid infrastructure.
        services.Configure(configureMongoDb);
        services.Configure(configureJwt);
        services.AddSingleton<IMongoClient>(serviceProvider =>
        {
            // Create the MongoDB client from configured connection settings.
            var options = serviceProvider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            ValidateMongoDbOptions(options);
            return new MongoClient(options.ConnectionString);
        });
        services.AddSingleton(serviceProvider =>
        {
            // Resolve the configured MongoDB database for repository implementations.
            var options = serviceProvider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            ValidateMongoDbOptions(options);
            var mongoClient = serviceProvider.GetRequiredService<IMongoClient>();
            return mongoClient.GetDatabase(options.DatabaseName);
        });
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IReservationQrTokenService, Sha256ReservationQrTokenService>();
        services.AddScoped<IUserRepository, MongoUserRepository>();
        services.AddScoped<IProsumerRepository, MongoProsumerRepository>();
        services.AddScoped<IReservationRepository, MongoReservationRepository>();
        services.AddScoped<ISolarStationRepository, MongoSolarStationRepository>();
        services.AddScoped<IUserCollectionInitializer, MongoUserCollectionInitializer>();
        services.AddScoped<IProsumerCollectionInitializer, MongoProsumerCollectionInitializer>();
        services.AddScoped<IReservationCollectionInitializer, MongoReservationCollectionInitializer>();
        services.AddScoped<ISolarStationCollectionInitializer, MongoSolarStationCollectionInitializer>();
        services.AddScoped<IUserSeedDataInitializer, MongoUserSeedDataInitializer>();

        return services;
    }

    private static void ValidateMongoDbOptions(MongoDbOptions options)
    {
        // Ensure MongoDB dependencies are configured through environment or user-secrets.
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException("MongoDB connection string is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.DatabaseName))
        {
            throw new InvalidOperationException("MongoDB database name is not configured.");
        }
    }
}
