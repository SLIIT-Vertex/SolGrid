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

public interface ISolarStationService : IBookingSlotService
{
    // Validate station details and code uniqueness before creating the node.
    Task<SolarStationResponse> CreateStationAsync(
        CreateSolarStationRequest request,
        CancellationToken cancellationToken = default);

    // Validate the complete edit before applying domain changes.
    Task<SolarStationResponse> UpdateStationAsync(
        string id,
        UpdateSolarStationRequest request,
        CancellationToken cancellationToken = default);

    // Return a station response or report the shared not-found application error.
    Task<SolarStationResponse> GetStationByIdAsync(string id, CancellationToken cancellationToken = default);

    // Validate filters and return the requested page of stations.
    Task<PagedResult<SolarStationResponse>> GetStationsAsync(
        SolarStationQuery query,
        CancellationToken cancellationToken = default);

    // Preserve the existing discovery contract for later implementation.
    Task<IReadOnlyList<SolarStationResponse>> GetNearbyStationsAsync(
        NearbyStationQuery query,
        CancellationToken cancellationToken = default);

    // Validate weekly windows and affected reservations before replacing the schedule.
    Task<SolarStationResponse> ReplaceScheduleAsync(
        string id,
        UpdateStationScheduleRequest request,
        CancellationToken cancellationToken = default);

    // Activate through the domain entity after server-side authorization.
    Task ActivateStationAsync(string id, CancellationToken cancellationToken = default);

    // Block deactivation when IStationReservationLookup reports any active reservation.
    Task DeactivateStationAsync(string id, CancellationToken cancellationToken = default);
}
