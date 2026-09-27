/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationExpiryService.cs
 * Description: Coordinates automatic expiry of overdue energy reservations.
 * Contributor: Dilshan Yapa S Y C T
 */

using SolGrid.Application.Reservations.Interfaces;

namespace SolGrid.Application.Reservations.Services;

public sealed class ReservationExpiryService : IReservationExpiryService
{
    private readonly IReservationRepository reservationRepository;
    private readonly TimeProvider timeProvider;

    public ReservationExpiryService(IReservationRepository reservationRepository, TimeProvider timeProvider)
    {
        // Capture persistence and UTC clock abstractions for deterministic expiry processing.
        this.reservationRepository = reservationRepository;
        this.timeProvider = timeProvider;
    }

    public Task<long> ExpireDueReservationsAsync(CancellationToken cancellationToken = default)
    {
        // Expire pending reservations at their schedule and approved reservations after the completion grace period.
        var nowUtc = timeProvider.GetUtcNow();
        return reservationRepository.ExpireDueReservationsAsync(
            nowUtc,
            nowUtc - ReservationExpiryPolicy.ApprovedCompletionGracePeriod,
            nowUtc,
            cancellationToken);
    }
}
