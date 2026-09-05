/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IReservationService.cs
 * Description: Defines energy slot reservation use cases.
 * Contributor: Dilshan Yapa S Y C T
 */

using SolGrid.Application.Common.Models;
using SolGrid.Application.Reservations.Requests;
using SolGrid.Application.Reservations.Responses;
using SolGrid.Application.Users.Interfaces;

namespace SolGrid.Application.Reservations.Interfaces;

public interface IReservationService
{
    Task<ReservationResponse> CreateReservationAsync(CreateReservationRequest request, CancellationToken cancellationToken = default);

    Task<ReservationResponse> UpdateReservationAsync(string id, UpdateReservationRequest request, CancellationToken cancellationToken = default);

    Task CancelReservationAsync(string id, CancellationToken cancellationToken = default);

    Task<ReservationResponse> GetReservationByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationResponse>> GetReservationsAsync(ReservationQuery query, CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationResponse>> GetMyReservationsAsync(ReservationQuery query, CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationResponse>> GetDashboardReservationsAsync(
        ReservationDashboardView view,
        ReservationQuery query,
        CancellationToken cancellationToken = default);

    Task<ReservationDashboardSummaryResponse> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);

    Task<ReservationResponse> ApproveReservationAsync(string id, ApproveReservationRequest request, CancellationToken cancellationToken = default);

    Task<ReservationResponse> RejectReservationAsync(string id, RejectReservationRequest request, CancellationToken cancellationToken = default);

    Task<ReservationQrResponse> IssueReservationQrAsync(string id, CancellationToken cancellationToken = default);

    Task<VerifyReservationQrResponse> VerifyReservationQrAsync(VerifyReservationQrRequest request, CancellationToken cancellationToken = default);

    Task<ReservationResponse> CompleteReservationAsync(string id, CompleteReservationRequest request, CancellationToken cancellationToken = default);
}
