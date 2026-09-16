/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IBookingSlotService.cs
 * Description: Defines authoritative energy booking slot management operations.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.Common.Models;
using SolGrid.Application.SolarStations.Requests;
using SolGrid.Application.SolarStations.Responses;

namespace SolGrid.Application.SolarStations.Interfaces;

public interface IBookingSlotService
{
    // Validate station existence, activity, schedule, and uniqueness before creating a slot.
    Task<EnergyBookingSlotResponse> CreateBookingSlotAsync(
        string stationId,
        CreateBookingSlotRequest request,
        CancellationToken cancellationToken = default);

    // Protect identity and ownership while replacing editable capacity and time values.
    Task<EnergyBookingSlotResponse> UpdateBookingSlotAsync(
        string id,
        UpdateBookingSlotRequest request,
        CancellationToken cancellationToken = default);

    // Return one globally identified slot to authenticated booking clients without exposing persistence types.
    Task<EnergyBookingSlotResponse> GetBookingSlotByIdAsync(
        string id, CancellationToken cancellationToken = default);

    // Validate filters, confirm the owning station exists, and return one page of slots to authenticated clients.
    Task<PagedResult<EnergyBookingSlotResponse>> GetBookingSlotsAsync(
        BookingSlotQuery query, CancellationToken cancellationToken = default);

    // Return an out-of-service slot to the bookable pool through the domain transition.
    Task ActivateBookingSlotAsync(string id, CancellationToken cancellationToken = default);

    // Withdraw a slot from bookings without deleting historical reservation references.
    Task DeactivateBookingSlotAsync(string id, CancellationToken cancellationToken = default);
}
