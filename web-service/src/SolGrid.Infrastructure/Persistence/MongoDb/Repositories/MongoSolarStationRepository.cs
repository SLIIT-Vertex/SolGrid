/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: MongoSolarStationRepository.cs
 * Description: Implements solar station persistence using the SolarStationInfo collection.
 * Contributor: Kavishi Godage
 */

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Common.Models;
using SolGrid.Application.SolarStations.Interfaces;
using SolGrid.Application.SolarStations.Validation;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using SolGrid.Domain.ValueObjects;
using SolGrid.Infrastructure.Persistence.MongoDb.Documents;
using System.Text.RegularExpressions;

namespace SolGrid.Infrastructure.Persistence.MongoDb.Repositories;

public sealed class MongoSolarStationRepository : ISolarStationRepository
{
    private const int DefaultPageNumber = 1;
    private const int MaximumPageSize = 100;

    private readonly IMongoCollection<SolarStationDocument> stationsCollection;

    public MongoSolarStationRepository(IMongoDatabase database, IOptions<MongoDbOptions> options)
    {
        // Resolve the SolarStationInfo collection from configured MongoDB options.
        stationsCollection = database.GetCollection<SolarStationDocument>(options.Value.SolarStationInfoCollectionName);
    }

    public async Task<SolarStation?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        // Find a station by its persistent identifier.
        var document = await stationsCollection
            .Find(station => station.Id == id.Trim())
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return document?.ToDomain();
    }

    public async Task<SolarStation?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        // Find a station by its normalized unique code.
        var document = await stationsCollection
            .Find(station => station.Code == NormalizeCode(code))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return document?.ToDomain();
    }

    public async Task<IReadOnlyList<SolarStation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Return all stations ordered by creation date for administrative lists.
        var documents = await stationsCollection
            .Find(FilterDefinition<SolarStationDocument>.Empty)
            .SortByDescending(station => station.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return documents.Select(document => document.ToDomain()).ToArray();
    }

    public async Task<PagedResult<SolarStation>> GetPagedAsync(
        SolarStationQuery query,
        CancellationToken cancellationToken = default)
    {
        // Return filtered stations with bounded paging for management screens.
        var pageNumber = Math.Max(query.PageNumber, DefaultPageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, MaximumPageSize);
        var filter = BuildFilter(query);
        var skip = (pageNumber - 1) * pageSize;

        var totalCountTask = stationsCollection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var documentsTask = stationsCollection
            .Find(filter)
            .SortByDescending(station => station.CreatedAtUtc)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        await Task.WhenAll(totalCountTask, documentsTask).ConfigureAwait(false);

        return new PagedResult<SolarStation>
        {
            Items = documentsTask.Result.Select(document => document.ToDomain()).ToArray(),
            TotalCount = totalCountTask.Result,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<SolarStation>> GetNearbyAsync(
        NearbyStationQuery query,
        CancellationToken cancellationToken = default)
    {
        // Rank stations inside the search radius using the 2dsphere geo index.
        var maxResults = Math.Clamp(query.MaxResults, 1, SolarStationValidationRules.MaximumNearbyResults);
        var origin = GeoJson.Point(new GeoJson2DGeographicCoordinates(query.Longitude, query.Latitude));
        var builder = Builders<SolarStationDocument>.Filter;
        var filter = builder.NearSphere(
            station => station.GeoLocation,
            origin,
            maxDistance: query.RadiusKilometers * 1000d);

        if (query.ActiveOnly)
        {
            filter &= builder.Eq(station => station.Status, StationStatus.Active);
        }

        var documents = await stationsCollection
            .Find(filter)
            .Limit(maxResults)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var originCoordinates = GeoCoordinates.Create(query.Latitude, query.Longitude);
        return documents
            .Select(document => document.ToDomain())
            .Where(station => station.DistanceInKilometersFrom(originCoordinates) <= query.RadiusKilometers)
            .Take(maxResults)
            .ToArray();
    }

    public async Task<bool> ExistsByCodeAsync(
        string code,
        string? excludingStationId = null,
        CancellationToken cancellationToken = default)
    {
        // Check whether a normalized station code is already assigned.
        var filter = Builders<SolarStationDocument>.Filter.Eq(station => station.Code, NormalizeCode(code));

        if (!string.IsNullOrWhiteSpace(excludingStationId))
        {
            filter &= Builders<SolarStationDocument>.Filter.Ne(station => station.Id, excludingStationId.Trim());
        }

        return await stationsCollection
            .Find(filter)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(SolarStation station, CancellationToken cancellationToken = default)
    {
        // Insert a station document and translate uniqueness races to an application conflict.
        try
        {
            await stationsCollection
                .InsertOneAsync(SolarStationDocument.FromDomain(station), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new ConflictException("Station code is already assigned to another station.");
        }
    }

    public async Task UpdateAsync(SolarStation station, CancellationToken cancellationToken = default)
    {
        // Replace an existing station document while preserving its identifier.
        try
        {
            var result = await stationsCollection
                .ReplaceOneAsync(
                    existingStation => existingStation.Id == station.Id,
                    SolarStationDocument.FromDomain(station),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (result.MatchedCount == 0)
            {
                throw new InvalidOperationException("Station could not be found for update.");
            }
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new ConflictException("Station code is already assigned to another station.");
        }
    }

    private static FilterDefinition<SolarStationDocument> BuildFilter(SolarStationQuery query)
    {
        // Build a MongoDB filter from application-level station query values.
        var builder = Builders<SolarStationDocument>.Filter;
        var filters = new List<FilterDefinition<SolarStationDocument>>();

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var searchText = Regex.Escape(query.SearchText.Trim());
            filters.Add(builder.Or(
                builder.Regex(station => station.Code, new MongoDB.Bson.BsonRegularExpression(searchText, "i")),
                builder.Regex(station => station.Name, new MongoDB.Bson.BsonRegularExpression(searchText, "i")),
                builder.Regex(station => station.AddressLine, new MongoDB.Bson.BsonRegularExpression(searchText, "i"))));
        }

        if (query.Status.HasValue)
        {
            filters.Add(builder.Eq(station => station.Status, query.Status.Value));
        }

        if (query.HasAvailableSlots == true)
        {
            filters.Add(builder.Gt(station => station.AvailableSlotCount, 0));
        }
        else if (query.HasAvailableSlots == false)
        {
            filters.Add(builder.Eq(station => station.AvailableSlotCount, 0));
        }

        return filters.Count == 0 ? builder.Empty : builder.And(filters);
    }

    private static string NormalizeCode(string code)
    {
        // Normalize station codes consistently with the domain model.
        return code.Trim().ToUpperInvariant();
    }
}
