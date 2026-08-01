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
        // Create indexes needed by reservation queries and slot conflict checks.
        var indexes = new[]
        {
            new CreateIndexModel<ReservationDocument>(
                Builders<ReservationDocument>.IndexKeys
                    .Ascending(reservation => reservation.BookingSlotId)
                    .Ascending(reservation => reservation.Status),
                new CreateIndexOptions
                {
                    Name = "ix_energy_reservations_slot_status"
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
                    .Ascending(reservation => reservation.Status),
                new CreateIndexOptions
                {
                    Name = "ix_energy_reservations_station_status"
                })
        };

        await reservationsCollection.Indexes.CreateManyAsync(indexes, cancellationToken).ConfigureAwait(false);
    }
}
