/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: MongoReservationCollectionInitializer.cs
 * Description: Creates MongoDB indexes required for energy reservation persistence.
 * Contributor: Dilshan Yapa S Y C T
 */

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SolGrid.Infrastructure.Persistence.MongoDb.Documents;
using SolGrid.Domain.Enums;

namespace SolGrid.Infrastructure.Persistence.MongoDb;

public interface IReservationCollectionInitializer
{
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
}

public sealed class MongoReservationCollectionInitializer : IReservationCollectionInitializer
{
    private readonly IMongoCollection<ReservationDocument> reservationsCollection;

    public MongoReservationCollectionInitializer(IMongoDatabase database, IOptions<MongoDbOptions> options)
    {
        // Resolve the EnergyReservations collection from configured MongoDB options.
        reservationsCollection = database.GetCollection<ReservationDocument>(options.Value.EnergyReservationsCollectionName);
    }

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        // Backfill concurrency and active-slot fields before creating reservation indexes.
        await BackfillConcurrencyFieldsAsync(cancellationToken).ConfigureAwait(false);

        // Create indexes needed by reservation queries and slot conflict checks.
        var indexes = new[]
        {
            new CreateIndexModel<ReservationDocument>(
                Builders<ReservationDocument>.IndexKeys
                    .Ascending(reservation => reservation.BookingSlotId),
                new CreateIndexOptions<ReservationDocument>
                {
                    Name = "ux_energy_reservations_active_slot",
                    Unique = true,
                    PartialFilterExpression = Builders<ReservationDocument>.Filter
                        .Eq(reservation => reservation.IsActiveForBookingSlot, true)
                }),
            new CreateIndexModel<ReservationDocument>(
                Builders<ReservationDocument>.IndexKeys
                    .Ascending(reservation => reservation.ProsumerId)
                    .Ascending(reservation => reservation.ScheduledAtUtc),
                new CreateIndexOptions
                {
                    Name = "ix_energy_reservations_prosumer_scheduled_at"
                }),
            new CreateIndexModel<ReservationDocument>(
                Builders<ReservationDocument>.IndexKeys
                    .Ascending(reservation => reservation.StationId)
                    .Ascending(reservation => reservation.Status)
                    .Ascending(reservation => reservation.ScheduledAtUtc),
                new CreateIndexOptions
                {
                    Name = "ix_energy_reservations_station_status_scheduled_at"
                }),
            new CreateIndexModel<ReservationDocument>(
                Builders<ReservationDocument>.IndexKeys
                    .Ascending(reservation => reservation.Status)
                    .Ascending(reservation => reservation.ScheduledAtUtc),
                new CreateIndexOptions
                {
                    Name = "ix_energy_reservations_status_scheduled_at"
                })
        };

        await reservationsCollection.Indexes.CreateManyAsync(indexes, cancellationToken).ConfigureAwait(false);
    }

    private async Task BackfillConcurrencyFieldsAsync(CancellationToken cancellationToken)
    {
        // Preserve compatibility with reservation documents created before concurrency hardening.
        var builder = Builders<ReservationDocument>.Filter;
        var missingVersion = builder.Exists(nameof(ReservationDocument.Version), false);
        var missingActiveSlotFlag = builder.Exists(nameof(ReservationDocument.IsActiveForBookingSlot), false);

        await reservationsCollection.UpdateManyAsync(
            missingVersion,
            Builders<ReservationDocument>.Update.Set(reservation => reservation.Version, 0),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        await reservationsCollection.UpdateManyAsync(
            missingActiveSlotFlag & builder.In(
                reservation => reservation.Status,
                [ReservationStatus.Pending, ReservationStatus.Approved]),
            Builders<ReservationDocument>.Update.Set(reservation => reservation.IsActiveForBookingSlot, true),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        await reservationsCollection.UpdateManyAsync(
            missingActiveSlotFlag & builder.Nin(
                reservation => reservation.Status,
                [ReservationStatus.Pending, ReservationStatus.Approved]),
            Builders<ReservationDocument>.Update.Set(reservation => reservation.IsActiveForBookingSlot, false),
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
