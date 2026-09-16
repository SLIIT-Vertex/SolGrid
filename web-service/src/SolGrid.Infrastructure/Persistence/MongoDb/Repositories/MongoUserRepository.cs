/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: MongoUserRepository.cs
 * Description: Implements web user persistence using MongoDB.
 * Contributor: Bawanthi K D R
 */

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SolGrid.Application.Common.Models;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Domain.Entities;
using SolGrid.Infrastructure.Persistence.MongoDb.Documents;
using System.Text.RegularExpressions;

namespace SolGrid.Infrastructure.Persistence.MongoDb.Repositories;

public sealed class MongoUserRepository : IUserRepository
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    private readonly IMongoCollection<UserDocument> usersCollection;

    public MongoUserRepository(IMongoDatabase database, IOptions<MongoDbOptions> options)
    {
        // Resolve the Users collection from configured MongoDB options.
        usersCollection = database.GetCollection<UserDocument>(options.Value.UsersCollectionName);
    }

    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        // Find a user by its persistent identifier.
        var document = await usersCollection
            .Find(user => user.Id == id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return document?.ToDomain();
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // Find a user by normalized email.
        var normalizedEmail = NormalizeEmail(email);
        var document = await usersCollection
            .Find(user => user.Email == normalizedEmail)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return document?.ToDomain();
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Return all users ordered by creation date for administrative lists.
        var documents = await usersCollection
            .Find(FilterDefinition<UserDocument>.Empty)
            .SortByDescending(user => user.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return documents.Select(document => document.ToDomain()).ToArray();
    }

    public async Task<PagedResult<User>> GetPagedAsync(UserQuery query, CancellationToken cancellationToken = default)
    {
        // Return filtered users with bounded paging for administrative screens.
        var pageNumber = Math.Max(query.PageNumber, DefaultPageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, MaximumPageSize);
        var filter = BuildFilter(query);
        var skip = (pageNumber - 1) * pageSize;

        var totalCountTask = usersCollection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var documentsTask = usersCollection
            .Find(filter)
            .SortByDescending(user => user.CreatedAtUtc)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        await Task.WhenAll(totalCountTask, documentsTask).ConfigureAwait(false);

        return new PagedResult<User>
        {
            Items = documentsTask.Result.Select(document => document.ToDomain()).ToArray(),
            TotalCount = totalCountTask.Result,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<bool> ExistsByEmailAsync(
        string email,
        string? excludingUserId = null,
        CancellationToken cancellationToken = default)
    {
        // Check whether an email is already assigned to another user.
        var normalizedEmail = NormalizeEmail(email);
        var filter = Builders<UserDocument>.Filter.Eq(user => user.Email, normalizedEmail);

        if (!string.IsNullOrWhiteSpace(excludingUserId))
        {
            filter &= Builders<UserDocument>.Filter.Ne(user => user.Id, excludingUserId);
        }

        return await usersCollection
            .Find(filter)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> EmailExistsAsync(
        string email,
        string? excludingUserId = null,
        CancellationToken cancellationToken = default)
    {
        // Preserve the Phase 1 contract name while routing to the clearer exists method.
        return ExistsByEmailAsync(email, excludingUserId, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        // Insert a new user document into MongoDB.
        await usersCollection
            .InsertOneAsync(UserDocument.FromDomain(user), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        // Replace an existing user document while preserving the same identifier.
        var result = await usersCollection
            .ReplaceOneAsync(
                existingUser => existingUser.Id == user.Id,
                UserDocument.FromDomain(user),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (result.MatchedCount == 0)
        {
            throw new InvalidOperationException("User could not be found for update.");
        }
    }

    private static FilterDefinition<UserDocument> BuildFilter(UserQuery query)
    {
        // Build a MongoDB filter from application-level query values.
        var builder = Builders<UserDocument>.Filter;
        var filters = new List<FilterDefinition<UserDocument>>();

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var searchText = Regex.Escape(query.SearchText.Trim());
            filters.Add(builder.Or(
                builder.Regex(user => user.FirstName, new MongoDB.Bson.BsonRegularExpression(searchText, "i")),
                builder.Regex(user => user.LastName, new MongoDB.Bson.BsonRegularExpression(searchText, "i")),
                builder.Regex(user => user.Email, new MongoDB.Bson.BsonRegularExpression(searchText, "i"))));
        }

        if (query.Role.HasValue)
        {
            filters.Add(builder.Eq(user => user.Role, query.Role.Value));
        }

        if (query.Status.HasValue)
        {
            filters.Add(builder.Eq(user => user.AccountStatus, query.Status.Value));
        }

        return filters.Count == 0 ? builder.Empty : builder.And(filters);
    }

    private static string NormalizeEmail(string email)
    {
        // Normalize email consistently with the domain model.
        return email.Trim().ToLowerInvariant();
    }
}
