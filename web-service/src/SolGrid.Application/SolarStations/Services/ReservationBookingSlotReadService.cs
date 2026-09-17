/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationBookingSlotReadService.cs
 * Description: Adapts booking slot data to the reservation component's focused read contract.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.Reservations.Interfaces;
using SolGrid.Application.SolarStations.Interfaces;
using SolGrid.Domain.Enums;

namespace SolGrid.Application.SolarStations.Services;

public sealed class ReservationBookingSlotReadService : IReservationBookingSlotReadService
{
    private readonly IBookingSlotRepository bookingSlotRepository;
    private readonly TimeProvider timeProvider;

    public ReservationBookingSlotReadService(IBookingSlotRepository bookingSlotRepository, TimeProvider timeProvider)
    {
        // Capture the slot abstraction without exposing persistence implementation details.
        this.bookingSlotRepository = bookingSlotRepository;
        this.timeProvider = timeProvider;
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

    public async Task ReserveAsync(string bookingSlotId, CancellationToken cancellationToken = default)
    {
        // Hold the slot so it stops appearing bookable while its reservation is pending review or approved.
        var slot = await bookingSlotRepository
            .GetByIdAsync(bookingSlotId.Trim(), cancellationToken)
            .ConfigureAwait(false);

        if (slot is null || !slot.IsAvailable)
        {
            return;
        }

        slot.Reserve(timeProvider.GetUtcNow());
        await bookingSlotRepository.UpdateAsync(slot, cancellationToken).ConfigureAwait(false);
    }

    public async Task OccupyAsync(string bookingSlotId, CancellationToken cancellationToken = default)
    {
        // Mark the slot physically in use once the prosumer's arrival QR is verified.
        var slot = await bookingSlotRepository
            .GetByIdAsync(bookingSlotId.Trim(), cancellationToken)
            .ConfigureAwait(false);

        if (slot is null || slot.Status != SlotStatus.Reserved)
        {
            return;
        }

        slot.MarkOccupied(timeProvider.GetUtcNow());
        await bookingSlotRepository.UpdateAsync(slot, cancellationToken).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(string bookingSlotId, CancellationToken cancellationToken = default)
    {
        // Return the slot to the bookable pool once its committed reservation ends.
        var slot = await bookingSlotRepository
            .GetByIdAsync(bookingSlotId.Trim(), cancellationToken)
            .ConfigureAwait(false);

        if (slot is null || !slot.IsCommitted)
        {
            return;
        }

        slot.Release(timeProvider.GetUtcNow());
        await bookingSlotRepository.UpdateAsync(slot, cancellationToken).ConfigureAwait(false);
    }
}
