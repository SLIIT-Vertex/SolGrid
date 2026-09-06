/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationBookingSlotReadService.cs
 * Description: Adapts booking slot data to the reservation component's focused read contract.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Application.SolarStations.Interfaces;

namespace SolGrid.Application.SolarStations.Services;

public sealed class ReservationBookingSlotReadService : IReservationBookingSlotReadService
{
    private readonly IBookingSlotRepository bookingSlotRepository;

    public ReservationBookingSlotReadService(IBookingSlotRepository bookingSlotRepository)
    {
        // Capture the slot abstraction without exposing persistence implementation details.
        this.bookingSlotRepository = bookingSlotRepository;
    }

    public async Task<ReservationBookingSlotSnapshot?> GetByIdAsync(
        string bookingSlotId,
        CancellationToken cancellationToken = default)
    {
        // Return only the identity, ownership, and eligibility snapshot required by reservations.
        if (string.IsNullOrWhiteSpace(bookingSlotId))
        {
            return null;
        }

        var slot = await bookingSlotRepository
            .GetByIdAsync(bookingSlotId.Trim(), cancellationToken)
            .ConfigureAwait(false);

        return slot is null
            ? null
            : new ReservationBookingSlotSnapshot
            {
                Id = slot.Id,
                StationId = slot.StationId,
                IsActive = slot.IsActive,
                IsAvailable = slot.IsAvailable
            };
    }
}
