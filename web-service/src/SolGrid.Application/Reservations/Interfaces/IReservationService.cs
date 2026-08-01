/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IReservationService.cs
 * Description: Defines energy slot reservation use cases.
 * Contributor: Dilshan Yapa S Y C T
 */

using SolGrid.Application.Reservations.Requests;
using SolGrid.Application.Reservations.Responses;

namespace SolGrid.Application.Reservations.Interfaces;

public interface IReservationService
{
    Task<ReservationResponse> CreateReservationAsync(CreateReservationRequest request, CancellationToken cancellationToken = default);
}
