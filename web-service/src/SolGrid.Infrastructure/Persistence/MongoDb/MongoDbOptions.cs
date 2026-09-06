/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: MongoDbOptions.cs
 * Description: Holds MongoDB persistence configuration for SolGrid infrastructure.
 * Contributor: Bawanthi K D R
 */

namespace SolGrid.Infrastructure.Persistence.MongoDb;

public sealed class MongoDbOptions
{
    public string ConnectionString { get; init; } = string.Empty;

    public string DatabaseName { get; init; } = "SolGrid";

    public string UsersCollectionName { get; init; } = "Users";

    public string ProsumersCollectionName { get; init; } = "Prosumers";

    public string EnergyReservationsCollectionName { get; init; } = "EnergyReservations";

    public string SolarStationInfoCollectionName { get; init; } = "SolarStationInfo";

    public string EnergyBookingSlotsCollectionName { get; init; } = "EnergyBookingSlots";

    public bool InitializeOnStartup { get; init; } = true;

    public bool SeedDevelopmentUsers { get; init; }

    public SeedUserOptions? BackofficeSeedUser { get; init; }

    public SeedUserOptions? GridOperatorSeedUser { get; init; }
}
