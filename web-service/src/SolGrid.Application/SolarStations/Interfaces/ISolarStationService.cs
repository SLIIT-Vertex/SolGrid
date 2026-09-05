/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ISolarStationService.cs
 * Description: Defines microgrid solar station management use cases.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.Common.Models;
using SolGrid.Application.SolarStations.Requests;
using SolGrid.Application.SolarStations.Responses;

namespace SolGrid.Application.SolarStations.Interfaces;

public interface ISolarStationService
{
    Task<SolarStationResponse> CreateStationAsync(
        CreateSolarStationRequest request,
        CancellationToken cancellationToken = default);

    Task<SolarStationResponse> UpdateStationAsync(
        string id,
        UpdateSolarStationRequest request,
        CancellationToken cancellationToken = default);

    Task<SolarStationResponse> GetStationByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<PagedResult<SolarStationResponse>> GetStationsAsync(
        SolarStationQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SolarStationResponse>> GetNearbyStationsAsync(
        NearbyStationQuery query,
        CancellationToken cancellationToken = default);

    Task<SolarStationResponse> ReplaceScheduleAsync(
        string id,
        UpdateStationScheduleRequest request,
        CancellationToken cancellationToken = default);

    Task<EnergyBookingSlotResponse> AddSlotAsync(
        string id,
        EnergyBookingSlotRequest request,
        CancellationToken cancellationToken = default);

    Task<EnergyBookingSlotResponse> UpdateSlotStatusAsync(
        string id,
        string slotId,
        UpdateSlotStatusRequest request,
        CancellationToken cancellationToken = default);

    Task RemoveSlotAsync(string id, string slotId, CancellationToken cancellationToken = default);

    Task ActivateStationAsync(string id, CancellationToken cancellationToken = default);

    Task DeactivateStationAsync(string id, CancellationToken cancellationToken = default);
}
