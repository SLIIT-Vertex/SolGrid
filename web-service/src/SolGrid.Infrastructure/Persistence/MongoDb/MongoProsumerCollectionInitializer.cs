/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: MongoProsumerCollectionInitializer.cs
 * Description: Creates MongoDB indexes required for prosumer persistence.
 * Contributor: Gunasekara H N
 */

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SolGrid.Infrastructure.Persistence.MongoDb.Documents;

namespace SolGrid.Infrastructure.Persistence.MongoDb;

public interface IProsumerCollectionInitializer
{
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
}

public sealed class MongoProsumerCollectionInitializer : IProsumerCollectionInitializer
{
    private readonly IMongoCollection<ProsumerDocument> prosumersCollection;

    public MongoProsumerCollectionInitializer(IMongoDatabase database, IOptions<MongoDbOptions> options)
    {
        // Resolve the Prosumers collection from configured MongoDB options.
        prosumersCollection = database.GetCollection<ProsumerDocument>(options.Value.ProsumersCollectionName);
    }

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        // Create email and status indexes; MongoDB's required _id index enforces NIC uniqueness.
        var indexes = new[]
        {
            new CreateIndexModel<ProsumerDocument>(
                Builders<ProsumerDocument>.IndexKeys.Ascending(prosumer => prosumer.Email),
                new CreateIndexOptions
                {
                    Name = "ux_prosumers_email",
                    Unique = true
                }),
            new CreateIndexModel<ProsumerDocument>(
                Builders<ProsumerDocument>.IndexKeys.Ascending(prosumer => prosumer.AccountStatus),
                new CreateIndexOptions
                {
                    Name = "ix_prosumers_account_status"
                })
        };

        await prosumersCollection.Indexes.CreateManyAsync(indexes, cancellationToken).ConfigureAwait(false);
    }
}
