/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: MongoUserCollectionInitializer.cs
 * Description: Creates MongoDB indexes required for web user persistence.
 * Contributor: Bawanthi K D R
 */

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SolGrid.Infrastructure.Persistence.MongoDb.Documents;

namespace SolGrid.Infrastructure.Persistence.MongoDb;

public interface IUserCollectionInitializer
{
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
}

public sealed class MongoUserCollectionInitializer : IUserCollectionInitializer
{
    private readonly IMongoCollection<UserDocument> usersCollection;

    public MongoUserCollectionInitializer(IMongoDatabase database, IOptions<MongoDbOptions> options)
    {
        // Resolve the Users collection from configured MongoDB options.
        usersCollection = database.GetCollection<UserDocument>(options.Value.UsersCollectionName);
    }

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        // Create indexes needed by user management queries and uniqueness rules.
        var indexes = new[]
        {
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(user => user.Email),
                new CreateIndexOptions
                {
                    Name = "ux_users_email",
                    Unique = true
                }),
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(user => user.Role),
                new CreateIndexOptions
                {
                    Name = "ix_users_role"
                }),
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(user => user.AccountStatus),
                new CreateIndexOptions
                {
                    Name = "ix_users_account_status"
                })
        };

        await usersCollection.Indexes.CreateManyAsync(indexes, cancellationToken).ConfigureAwait(false);
    }
}
