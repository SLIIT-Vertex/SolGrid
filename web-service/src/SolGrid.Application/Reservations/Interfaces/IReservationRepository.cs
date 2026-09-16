/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IReservationRepository.cs
 * Description: Defines persistence operations required by energy reservation services.
 * Contributor: Dilshan Yapa S Y C T
 */

using SolGrid.Application.Users.Interfaces;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;

namespace SolGrid.Application.Reservations.Interfaces;

public interface IReservationRepository
{
    Task<EnergyReservation?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnergyReservation>> GetByProsumerIdAsync(string prosumerId, CancellationToken cancellationToken = default);

    Task<PagedResult<EnergyReservation>> GetPagedAsync(ReservationQuery query, CancellationToken cancellationToken = default);

    Task<PagedResult<EnergyReservation>> GetDashboardReservationsAsync(
        ReservationDashboardView view,
        ReservationQuery query,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task<ReservationDashboardCounts> GetDashboardCountsAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default);

    Task<bool> HasActiveReservationForBookingSlotAsync(string bookingSlotId, string? excludingReservationId = null, CancellationToken cancellationToken = default);

    Task<bool> HasActiveReservationsForStationAsync(string stationId, CancellationToken cancellationToken = default);

    Task AddAsync(EnergyReservation reservation, CancellationToken cancellationToken = default);

    Task UpdateAsync(EnergyReservation reservation, CancellationToken cancellationToken = default);
}

public sealed class ReservationQuery
{
    public string? ProsumerId { get; init; }

    public string? StationId { get; init; }

    public string? BookingSlotId { get; init; }

    public ReservationStatus? Status { get; init; }

    public string? SearchText { get; init; }

    public DateTimeOffset? ScheduledFrom { get; init; }

    public DateTimeOffset? ScheduledTo { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
