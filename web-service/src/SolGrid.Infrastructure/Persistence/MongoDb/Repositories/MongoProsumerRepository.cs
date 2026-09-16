/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: MongoProsumerRepository.cs
 * Description: Implements solar prosumer persistence using MongoDB.
 * Contributor: Gunasekara H N
 */

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Common.Models;
using SolGrid.Application.Prosumers.Interfaces;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Domain.Entities;
using SolGrid.Infrastructure.Persistence.MongoDb.Documents;
using System.Text.RegularExpressions;

namespace SolGrid.Infrastructure.Persistence.MongoDb.Repositories;

public sealed class MongoProsumerRepository : IProsumerRepository
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    private readonly IMongoCollection<ProsumerDocument> prosumersCollection;

    public MongoProsumerRepository(IMongoDatabase database, IOptions<MongoDbOptions> options)
    {
        // Resolve the Prosumers collection from configured MongoDB options.
        prosumersCollection = database.GetCollection<ProsumerDocument>(options.Value.ProsumersCollectionName);
    }

    public async Task<Prosumer?> GetByNicAsync(string nic, CancellationToken cancellationToken = default)
    {
        // Find a prosumer by its normalized NIC business identifier.
        var document = await prosumersCollection
            .Find(prosumer => prosumer.Nic == NormalizeNic(nic))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return document?.ToDomain();
    }

    public async Task<Prosumer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // Find a prosumer by normalized email.
        var document = await prosumersCollection
            .Find(prosumer => prosumer.Email == NormalizeEmail(email))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return document?.ToDomain();
    }

    public async Task<PagedResult<Prosumer>> GetPagedAsync(ProsumerQuery query, CancellationToken cancellationToken = default)
    {
        // Return filtered prosumers with bounded paging for future management screens.
        var pageNumber = Math.Max(query.PageNumber, DefaultPageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, MaximumPageSize);
        var filter = BuildFilter(query);
        var skip = (pageNumber - 1) * pageSize;

        var totalCountTask = prosumersCollection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var documentsTask = prosumersCollection
            .Find(filter)
            .SortByDescending(prosumer => prosumer.CreatedAtUtc)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        await Task.WhenAll(totalCountTask, documentsTask).ConfigureAwait(false);

        return new PagedResult<Prosumer>
        {
            Items = documentsTask.Result.Select(document => document.ToDomain()).ToArray(),
            TotalCount = totalCountTask.Result,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<bool> ExistsByNicAsync(string nic, CancellationToken cancellationToken = default)
    {
        // Check whether a normalized NIC is already registered.
        return await prosumersCollection
            .Find(prosumer => prosumer.Nic == NormalizeNic(nic))
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsByEmailAsync(
        string email,
        string? excludingNic = null,
        CancellationToken cancellationToken = default)
    {
        // Check whether an email is registered to another prosumer.
        var filter = Builders<ProsumerDocument>.Filter.Eq(prosumer => prosumer.Email, NormalizeEmail(email));

        if (!string.IsNullOrWhiteSpace(excludingNic))
        {
            filter &= Builders<ProsumerDocument>.Filter.Ne(prosumer => prosumer.Nic, NormalizeNic(excludingNic));
        }

        return await prosumersCollection
            .Find(filter)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Prosumer prosumer, CancellationToken cancellationToken = default)
    {
        // Insert a prosumer document and translate uniqueness races to an application conflict.
        try
        {
            await prosumersCollection
                .InsertOneAsync(ProsumerDocument.FromDomain(prosumer), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new ConflictException("NIC or email is already registered to another prosumer.");
        }
    }

    public async Task UpdateAsync(Prosumer prosumer, CancellationToken cancellationToken = default)
    {
        // Replace an existing prosumer document while preserving its immutable NIC identifier.
        try
        {
            var result = await prosumersCollection
                .ReplaceOneAsync(
                    existingProsumer => existingProsumer.Nic == prosumer.Nic,
                    ProsumerDocument.FromDomain(prosumer),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (result.MatchedCount == 0)
            {
                throw new InvalidOperationException("Prosumer could not be found for update.");
            }
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new ConflictException("Email is already registered to another prosumer.");
        }
    }

    private static FilterDefinition<ProsumerDocument> BuildFilter(ProsumerQuery query)
    {
        // Build a MongoDB filter from application-level prosumer query values.
        var builder = Builders<ProsumerDocument>.Filter;
        var filters = new List<FilterDefinition<ProsumerDocument>>();

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var searchText = Regex.Escape(query.SearchText.Trim());
            filters.Add(builder.Or(
                builder.Regex(prosumer => prosumer.Nic, new MongoDB.Bson.BsonRegularExpression(searchText, "i")),
                builder.Regex(prosumer => prosumer.FirstName, new MongoDB.Bson.BsonRegularExpression(searchText, "i")),
                builder.Regex(prosumer => prosumer.LastName, new MongoDB.Bson.BsonRegularExpression(searchText, "i")),
                builder.Regex(prosumer => prosumer.Email, new MongoDB.Bson.BsonRegularExpression(searchText, "i"))));
        }

        if (query.Status.HasValue)
        {
            filters.Add(builder.Eq(prosumer => prosumer.AccountStatus, query.Status.Value));
        }

        return filters.Count == 0 ? builder.Empty : builder.And(filters);
    }

    private static string NormalizeNic(string nic)
    {
        // Normalize NIC consistently with the prosumer domain model.
        return nic.Trim().ToUpperInvariant();
    }

    private static string NormalizeEmail(string email)
    {
        // Normalize email consistently with the prosumer domain model.
        return email.Trim().ToLowerInvariant();
    }
}
