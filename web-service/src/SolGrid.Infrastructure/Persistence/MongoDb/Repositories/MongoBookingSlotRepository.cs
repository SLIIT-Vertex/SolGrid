/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: MongoBookingSlotRepository.cs
 * Description: Implements energy booking slot persistence using the EnergyBookingSlots collection.
 * Contributor: Kavishi Godage
 */

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Common.Models;
using SolGrid.Application.SolarStations.Interfaces;
using SolGrid.Domain.Entities;
using SolGrid.Infrastructure.Persistence.MongoDb.Documents;

namespace SolGrid.Infrastructure.Persistence.MongoDb.Repositories;

public sealed class MongoBookingSlotRepository : IBookingSlotRepository
{
    private const int DefaultPageNumber = 1;
    private const int MaximumPageSize = 100;

    private readonly IMongoCollection<EnergyBookingSlotDocument> slotsCollection;

    public MongoBookingSlotRepository(IMongoDatabase database, IOptions<MongoDbOptions> options)
    {
        // Resolve the EnergyBookingSlots collection from configured MongoDB options.
        slotsCollection = database.GetCollection<EnergyBookingSlotDocument>(options.Value.EnergyBookingSlotsCollectionName);
    }

    public async Task<EnergyBookingSlot?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        // Find a booking slot by its persistent identifier.
        var document = await slotsCollection
            .Find(slot => slot.Id == id.Trim())
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return document?.ToDomain();
    }

    public async Task<PagedResult<EnergyBookingSlot>> GetPagedAsync(
        BookingSlotQuery query,
        CancellationToken cancellationToken = default)
    {
        // Return filtered slots with bounded paging for station management screens.
        var pageNumber = Math.Max(query.PageNumber, DefaultPageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, MaximumPageSize);
        var filter = BuildFilter(query);
        var skip = (pageNumber - 1) * pageSize;

        var totalCountTask = slotsCollection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var documentsTask = slotsCollection
            .Find(filter)
            .SortBy(slot => slot.StartTimeUtc)
            .ThenBy(slot => slot.SlotNumber)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        await Task.WhenAll(totalCountTask, documentsTask).ConfigureAwait(false);

        return new PagedResult<EnergyBookingSlot>
        {
            Items = documentsTask.Result.Select(document => document.ToDomain()).ToArray(),
            TotalCount = totalCountTask.Result,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task AddAsync(EnergyBookingSlot slot, CancellationToken cancellationToken = default)
    {
        // Insert a slot document and translate uniqueness races to an application conflict.
        try
        {
            await slotsCollection
                .InsertOneAsync(EnergyBookingSlotDocument.FromDomain(slot), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new ConflictException($"Slot number {slot.SlotNumber} is already used by this station.");
        }
    }

    public async Task UpdateAsync(EnergyBookingSlot slot, CancellationToken cancellationToken = default)
    {
        // Replace an existing slot document while preserving its identifier and station ownership.
        try
        {
            var result = await slotsCollection
                .ReplaceOneAsync(
                    existingSlot => existingSlot.Id == slot.Id && existingSlot.StationId == slot.StationId,
                    EnergyBookingSlotDocument.FromDomain(slot),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (result.MatchedCount == 0)
            {
                throw new InvalidOperationException("Booking slot could not be found for update.");
            }
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new ConflictException($"Slot number {slot.SlotNumber} is already used by this station.");
        }
    }

    public async Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        // Delete is retained for the repository contract but is not used by slot lifecycle APIs.
        var result = await slotsCollection
            .DeleteOneAsync(slot => slot.Id == id.Trim(), cancellationToken)
            .ConfigureAwait(false);

        if (result.DeletedCount == 0)
        {
            throw new InvalidOperationException("Booking slot could not be found for removal.");
        }
    }

    public async Task<bool> ExistsBySlotNumberAsync(
        string stationId,
        int slotNumber,
        string? excludingSlotId = null,
        CancellationToken cancellationToken = default)
    {
        // Check whether a station already has the requested slot number.
        var builder = Builders<EnergyBookingSlotDocument>.Filter;
        var filter = builder.Eq(slot => slot.StationId, stationId.Trim())
            & builder.Eq(slot => slot.SlotNumber, slotNumber);

        if (!string.IsNullOrWhiteSpace(excludingSlotId))
        {
            filter &= builder.Ne(slot => slot.Id, excludingSlotId.Trim());
        }

        return await slotsCollection
            .Find(filter)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> HasOverlappingIntervalAsync(
        string stationId,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string? excludingSlotId = null,
        CancellationToken cancellationToken = default)
    {
        // Detect another slot at the same station whose [start, end) overlaps the requested interval.
        var builder = Builders<EnergyBookingSlotDocument>.Filter;
        var filter = builder.Eq(slot => slot.StationId, stationId.Trim())
            & builder.Lt(slot => slot.StartTimeUtc, endTime.UtcDateTime)
            & builder.Gt(slot => slot.EndTimeUtc, startTime.UtcDateTime);

        if (!string.IsNullOrWhiteSpace(excludingSlotId))
        {
            filter &= builder.Ne(slot => slot.Id, excludingSlotId.Trim());
        }

        return await slotsCollection
            .Find(filter)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static FilterDefinition<EnergyBookingSlotDocument> BuildFilter(BookingSlotQuery query)
    {
        // Build a MongoDB filter from application-level booking slot query values.
        var builder = Builders<EnergyBookingSlotDocument>.Filter;
        var filters = new List<FilterDefinition<EnergyBookingSlotDocument>>();

        if (!string.IsNullOrWhiteSpace(query.StationId))
        {
            filters.Add(builder.Eq(slot => slot.StationId, query.StationId.Trim()));
        }

        if (query.Status.HasValue)
        {
            filters.Add(builder.Eq(slot => slot.Status, query.Status.Value));
        }

        if (query.From.HasValue)
        {
            filters.Add(builder.Gt(slot => slot.EndTimeUtc, query.From.Value.UtcDateTime));
        }

        if (query.To.HasValue)
        {
            filters.Add(builder.Lt(slot => slot.StartTimeUtc, query.To.Value.UtcDateTime));
        }

        return filters.Count == 0 ? builder.Empty : builder.And(filters);
    }
}
