/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: MongoReservationRepository.cs
 * Description: Implements energy reservation persistence using MongoDB.
 * Contributor: Dilshan Yapa S Y C T
 */

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SolGrid.Application.Common.Exceptions;
using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Application.Users.Interfaces;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using SolGrid.Infrastructure.Persistence.MongoDb.Documents;
using System.Text.RegularExpressions;

namespace SolGrid.Infrastructure.Persistence.MongoDb.Repositories;

public sealed class MongoReservationRepository : IReservationRepository
{
    private static readonly ReservationStatus[] ActiveStatuses =
    [
        ReservationStatus.Pending,
        ReservationStatus.Approved
    ];

    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    private readonly IMongoCollection<ReservationDocument> reservationsCollection;

    public MongoReservationRepository(IMongoDatabase database, IOptions<MongoDbOptions> options)
    {
        // Resolve the EnergyReservations collection from configured MongoDB options.
        reservationsCollection = database.GetCollection<ReservationDocument>(options.Value.EnergyReservationsCollectionName);
    }

    public async Task<EnergyReservation?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        // Find a reservation by its persistent identifier.
        var document = await reservationsCollection
            .Find(reservation => reservation.Id == id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return document?.ToDomain();
    }

    public async Task<IReadOnlyList<EnergyReservation>> GetByProsumerIdAsync(
        string prosumerId,
        CancellationToken cancellationToken = default)
    {
        // Return reservations for one prosumer ordered by schedule.
        var normalizedProsumerId = NormalizeIdentifier(prosumerId);
        var documents = await reservationsCollection
            .Find(reservation => reservation.ProsumerId == normalizedProsumerId)
            .SortBy(reservation => reservation.ScheduledAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return documents.Select(document => document.ToDomain()).ToArray();
    }

    public async Task<PagedResult<EnergyReservation>> GetPagedAsync(
        ReservationQuery query,
        CancellationToken cancellationToken = default)
    {
        // Return filtered reservations with bounded paging for future management screens.
        return await GetPagedForFilterAsync(BuildFilter(query), query, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<EnergyReservation>> GetDashboardReservationsAsync(
        ReservationDashboardView view,
        ReservationQuery query,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        // Return a server-filtered and paged reservation dashboard view.
        var builder = Builders<ReservationDocument>.Filter;
        var filter = builder.And(BuildFilter(query), BuildDashboardFilter(view, nowUtc));
        return await GetPagedForFilterAsync(filter, query, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReservationDashboardCounts> GetDashboardCountsAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        // Calculate dashboard counts directly in MongoDB without materializing reservations.
        var builder = Builders<ReservationDocument>.Filter;
        var scheduledAfterNow = builder.Gte(reservation => reservation.ScheduledAtUtc, nowUtc.UtcDateTime);
        var scheduledBeforeNow = builder.Lt(reservation => reservation.ScheduledAtUtc, nowUtc.UtcDateTime);
        var activeStatuses = builder.In(reservation => reservation.Status, ActiveStatuses);
        var terminalStatuses = builder.In(
            reservation => reservation.Status,
            [ReservationStatus.Rejected, ReservationStatus.Cancelled, ReservationStatus.Completed]);

        var pendingCountTask = reservationsCollection.CountDocumentsAsync(
            builder.Eq(reservation => reservation.Status, ReservationStatus.Pending), cancellationToken: cancellationToken);
        var approvedFutureCountTask = reservationsCollection.CountDocumentsAsync(
            builder.And(
                builder.Eq(reservation => reservation.Status, ReservationStatus.Approved),
                scheduledAfterNow), cancellationToken: cancellationToken);
        var currentCountTask = reservationsCollection.CountDocumentsAsync(
            builder.And(activeStatuses, scheduledAfterNow), cancellationToken: cancellationToken);
        var historyCountTask = reservationsCollection.CountDocumentsAsync(
            builder.Or(terminalStatuses, scheduledBeforeNow), cancellationToken: cancellationToken);

        await Task.WhenAll(pendingCountTask, approvedFutureCountTask, currentCountTask, historyCountTask)
            .ConfigureAwait(false);

        return new ReservationDashboardCounts
        {
            PendingReservationsCount = pendingCountTask.Result,
            ApprovedFutureReservationsCount = approvedFutureCountTask.Result,
            CurrentReservationsCount = currentCountTask.Result,
            BookingHistoryCount = historyCountTask.Result
        };
    }

    public async Task<bool> HasActiveReservationForBookingSlotAsync(
        string bookingSlotId,
        string? excludingReservationId = null,
        CancellationToken cancellationToken = default)
    {
        // Check whether a booking slot already has a pending or approved reservation.
        var builder = Builders<ReservationDocument>.Filter;
        var filter = builder.Eq(reservation => reservation.BookingSlotId, NormalizeIdentifier(bookingSlotId))
            & builder.In(reservation => reservation.Status, ActiveStatuses);

        if (!string.IsNullOrWhiteSpace(excludingReservationId))
        {
            filter &= builder.Ne(reservation => reservation.Id, excludingReservationId.Trim());
        }

        return await reservationsCollection
            .Find(filter)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> HasActiveReservationsForStationAsync(
        string stationId,
        CancellationToken cancellationToken = default)
    {
        // Check whether a station has any pending or approved reservations.
        var builder = Builders<ReservationDocument>.Filter;
        var filter = builder.Eq(reservation => reservation.StationId, NormalizeIdentifier(stationId))
            & builder.In(reservation => reservation.Status, ActiveStatuses);

        return await reservationsCollection
            .Find(filter)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(EnergyReservation reservation, CancellationToken cancellationToken = default)
    {
        // Insert a new reservation document and translate active-slot uniqueness conflicts.
        try
        {
            await reservationsCollection
                .InsertOneAsync(ReservationDocument.FromDomain(reservation), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new ConflictException("Booking slot already has an active reservation.");
        }
    }

    public async Task UpdateAsync(EnergyReservation reservation, CancellationToken cancellationToken = default)
    {
        // Replace an existing reservation document while preserving the same identifier.
        ReplaceOneResult result;

        try
        {
            result = await reservationsCollection
                .ReplaceOneAsync(
                    existingReservation => existingReservation.Id == reservation.Id
                        && existingReservation.Version == reservation.Version - 1,
                    ReservationDocument.FromDomain(reservation),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new ConflictException("Booking slot already has an active reservation.");
        }

        if (result.MatchedCount == 0)
        {
            throw new ConflictException("Reservation was changed by another request. Refresh and retry.");
        }
    }

    private static FilterDefinition<ReservationDocument> BuildFilter(ReservationQuery query)
    {
        // Build a MongoDB filter from application-level reservation query values.
        var builder = Builders<ReservationDocument>.Filter;
        var filters = new List<FilterDefinition<ReservationDocument>>();

        if (!string.IsNullOrWhiteSpace(query.ProsumerId))
        {
            filters.Add(builder.Eq(reservation => reservation.ProsumerId, NormalizeIdentifier(query.ProsumerId)));
        }

        if (!string.IsNullOrWhiteSpace(query.StationId))
        {
            filters.Add(builder.Eq(reservation => reservation.StationId, NormalizeIdentifier(query.StationId)));
        }

        if (!string.IsNullOrWhiteSpace(query.BookingSlotId))
        {
            filters.Add(builder.Eq(reservation => reservation.BookingSlotId, NormalizeIdentifier(query.BookingSlotId)));
        }

        if (query.Status.HasValue)
        {
            filters.Add(builder.Eq(reservation => reservation.Status, query.Status.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var searchText = Regex.Escape(query.SearchText.Trim());
            filters.Add(builder.Or(
                builder.Regex(reservation => reservation.Id, new MongoDB.Bson.BsonRegularExpression(searchText, "i")),
                builder.Regex(reservation => reservation.ProsumerId, new MongoDB.Bson.BsonRegularExpression(searchText, "i")),
                builder.Regex(reservation => reservation.StationId, new MongoDB.Bson.BsonRegularExpression(searchText, "i")),
                builder.Regex(reservation => reservation.BookingSlotId, new MongoDB.Bson.BsonRegularExpression(searchText, "i"))));
        }

        if (query.ScheduledFrom.HasValue)
        {
            filters.Add(builder.Gte(reservation => reservation.ScheduledAtUtc, query.ScheduledFrom.Value.UtcDateTime));
        }

        if (query.ScheduledTo.HasValue)
        {
            filters.Add(builder.Lte(reservation => reservation.ScheduledAtUtc, query.ScheduledTo.Value.UtcDateTime));
        }

        return filters.Count == 0 ? builder.Empty : builder.And(filters);
    }

    private async Task<PagedResult<EnergyReservation>> GetPagedForFilterAsync(
        FilterDefinition<ReservationDocument> filter,
        ReservationQuery query,
        CancellationToken cancellationToken)
    {
        // Execute a bounded page and total count against one MongoDB filter.
        var pageNumber = Math.Max(query.PageNumber, DefaultPageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, MaximumPageSize);
        var skip = (pageNumber - 1) * pageSize;

        var totalCountTask = reservationsCollection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var documentsTask = reservationsCollection
            .Find(filter)
            .SortBy(reservation => reservation.ScheduledAtUtc)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        await Task.WhenAll(totalCountTask, documentsTask).ConfigureAwait(false);

        return new PagedResult<EnergyReservation>
        {
            Items = documentsTask.Result.Select(document => document.ToDomain()).ToArray(),
            TotalCount = totalCountTask.Result,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private static FilterDefinition<ReservationDocument> BuildDashboardFilter(
        ReservationDashboardView view,
        DateTimeOffset nowUtc)
    {
        // Build the status and schedule filter that defines one dashboard view.
        var builder = Builders<ReservationDocument>.Filter;
        var scheduledAfterNow = builder.Gte(reservation => reservation.ScheduledAtUtc, nowUtc.UtcDateTime);

        return view switch
        {
            ReservationDashboardView.Current => builder.And(
                builder.In(reservation => reservation.Status, ActiveStatuses),
                scheduledAfterNow),
            ReservationDashboardView.Pending => builder.Eq(reservation => reservation.Status, ReservationStatus.Pending),
            ReservationDashboardView.History => builder.Or(
                builder.In(
                    reservation => reservation.Status,
                    [ReservationStatus.Rejected, ReservationStatus.Cancelled, ReservationStatus.Completed]),
                builder.Lt(reservation => reservation.ScheduledAtUtc, nowUtc.UtcDateTime)),
            _ => throw new ArgumentOutOfRangeException(nameof(view), view, "Reservation dashboard view is not supported.")
        };
    }

    private static string NormalizeIdentifier(string identifier)
    {
        // Normalize identifier values consistently before MongoDB filtering.
        return identifier.Trim();
    }
}
