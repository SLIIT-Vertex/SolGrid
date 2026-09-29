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
    private readonly IReservationBookingSlotReadService bookingSlotReadService;
    private readonly TimeProvider timeProvider;

    public ReservationExpiryService(
        IReservationRepository reservationRepository,
        IReservationBookingSlotReadService bookingSlotReadService,
        TimeProvider timeProvider)
    {
        // Capture persistence and UTC clock abstractions for deterministic expiry processing.
        this.reservationRepository = reservationRepository;
        this.bookingSlotReadService = bookingSlotReadService;
        this.timeProvider = timeProvider;
    }

    public async Task<long> ExpireDueReservationsAsync(CancellationToken cancellationToken = default)
    {
        // Expire due reservations, then return their committed slots to the bookable pool.
        var nowUtc = timeProvider.GetUtcNow();
        var expiredBookingSlotIds = await reservationRepository.ExpireDueReservationsAsync(
            nowUtc,
            nowUtc - ReservationExpiryPolicy.ApprovedCompletionGracePeriod,
            nowUtc,
            cancellationToken).ConfigureAwait(false);

        foreach (var bookingSlotId in expiredBookingSlotIds)
        {
            await bookingSlotReadService.ReleaseAsync(bookingSlotId, cancellationToken).ConfigureAwait(false);
        }

        return expiredBookingSlotIds.Count;
    }
}
