/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: MongoBookingSlotCollectionInitializer.cs
 * Description: Creates MongoDB indexes required for EnergyBookingSlots persistence.
 * Contributor: Kavishi Godage
 */

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SolGrid.Infrastructure.Persistence.MongoDb.Documents;

namespace SolGrid.Infrastructure.Persistence.MongoDb;

public interface IBookingSlotCollectionInitializer
{
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
}

public sealed class MongoBookingSlotCollectionInitializer : IBookingSlotCollectionInitializer
{
    private readonly IMongoCollection<EnergyBookingSlotDocument> slotsCollection;

    public MongoBookingSlotCollectionInitializer(IMongoDatabase database, IOptions<MongoDbOptions> options)
    {
        // Resolve the EnergyBookingSlots collection from configured MongoDB options.
        slotsCollection = database.GetCollection<EnergyBookingSlotDocument>(options.Value.EnergyBookingSlotsCollectionName);
    }

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        // Create uniqueness, station, time, and status indexes used by slot queries.
        var indexes = new[]
        {
            new CreateIndexModel<EnergyBookingSlotDocument>(
                Builders<EnergyBookingSlotDocument>.IndexKeys
                    .Ascending(slot => slot.StationId)
                    .Ascending(slot => slot.SlotNumber),
                new CreateIndexOptions
                {
                    Name = "ux_energy_booking_slots_station_slot_number",
                    Unique = true
                }),
            new CreateIndexModel<EnergyBookingSlotDocument>(
                Builders<EnergyBookingSlotDocument>.IndexKeys.Ascending(slot => slot.StationId),
                new CreateIndexOptions
                {
                    Name = "ix_energy_booking_slots_station_id"
                }),
            new CreateIndexModel<EnergyBookingSlotDocument>(
                Builders<EnergyBookingSlotDocument>.IndexKeys.Ascending(slot => slot.StartTimeUtc),
                new CreateIndexOptions
                {
                    Name = "ix_energy_booking_slots_start_time"
                }),
            new CreateIndexModel<EnergyBookingSlotDocument>(
                Builders<EnergyBookingSlotDocument>.IndexKeys.Ascending(slot => slot.Status),
                new CreateIndexOptions
                {
                    Name = "ix_energy_booking_slots_status"
                }),
            new CreateIndexModel<EnergyBookingSlotDocument>(
                Builders<EnergyBookingSlotDocument>.IndexKeys
                    .Ascending(slot => slot.StationId)
                    .Ascending(slot => slot.StartTimeUtc)
                    .Ascending(slot => slot.EndTimeUtc),
                new CreateIndexOptions
                {
                    Name = "ix_energy_booking_slots_station_start_end"
                })
        };

        await slotsCollection.Indexes.CreateManyAsync(indexes, cancellationToken).ConfigureAwait(false);
    }
}
