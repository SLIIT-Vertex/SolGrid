/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: MongoSolarStationCollectionInitializer.cs
 * Description: Creates MongoDB indexes required for SolarStationInfo persistence.
 * Contributor: Kavishi Godage
 */

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SolGrid.Infrastructure.Persistence.MongoDb.Documents;

namespace SolGrid.Infrastructure.Persistence.MongoDb;

public interface ISolarStationCollectionInitializer
{
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
}

public sealed class MongoSolarStationCollectionInitializer : ISolarStationCollectionInitializer
{
    private readonly IMongoCollection<SolarStationDocument> stationsCollection;

    public MongoSolarStationCollectionInitializer(IMongoDatabase database, IOptions<MongoDbOptions> options)
    {
        // Resolve the SolarStationInfo collection from configured MongoDB options.
        stationsCollection = database.GetCollection<SolarStationDocument>(options.Value.SolarStationInfoCollectionName);
    }

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        // Create only the indexes used by uniqueness, status filters, and nearby search.
        var indexes = new[]
        {
            new CreateIndexModel<SolarStationDocument>(
                Builders<SolarStationDocument>.IndexKeys.Ascending(station => station.Code),
                new CreateIndexOptions
                {
                    Name = "ux_solar_station_info_code",
                    Unique = true
                }),
            new CreateIndexModel<SolarStationDocument>(
                Builders<SolarStationDocument>.IndexKeys.Ascending(station => station.Status),
                new CreateIndexOptions
                {
                    Name = "ix_solar_station_info_status"
                }),
            new CreateIndexModel<SolarStationDocument>(
                Builders<SolarStationDocument>.IndexKeys.Geo2DSphere(station => station.GeoLocation),
                new CreateIndexOptions
                {
                    Name = "ix_solar_station_info_geo_location"
                })
        };

        await stationsCollection.Indexes.CreateManyAsync(indexes, cancellationToken).ConfigureAwait(false);
    }
}
