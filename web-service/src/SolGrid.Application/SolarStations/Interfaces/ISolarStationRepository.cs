/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ISolarStationRepository.cs
 * Description: Defines persistence operations required by solar station application services.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.Common.Models;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;

namespace SolGrid.Application.SolarStations.Interfaces;

public interface ISolarStationRepository
{
    Task<SolarStation?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<SolarStation?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SolarStation>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<SolarStation>> GetPagedAsync(SolarStationQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SolarStation>> GetNearbyAsync(NearbyStationQuery query, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(string code, string? excludingStationId = null, CancellationToken cancellationToken = default);

    Task AddAsync(SolarStation station, CancellationToken cancellationToken = default);

    Task UpdateAsync(SolarStation station, CancellationToken cancellationToken = default);
}

public sealed class SolarStationQuery
{
    public string? SearchText { get; init; }

    public StationStatus? Status { get; init; }

    public bool? HasAvailableSlots { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public sealed class NearbyStationQuery
{
    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public double RadiusKilometers { get; init; } = 10d;

    public bool ActiveOnly { get; init; } = true;

    public int MaxResults { get; init; } = 20;
}
