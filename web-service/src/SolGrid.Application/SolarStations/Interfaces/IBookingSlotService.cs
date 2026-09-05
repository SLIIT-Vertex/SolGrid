/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IBookingSlotService.cs
 * Description: Defines authoritative slot management operations for the future FAT service.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.Common.Models;
using SolGrid.Application.SolarStations.Requests;
using SolGrid.Application.SolarStations.Responses;

namespace SolGrid.Application.SolarStations.Interfaces;

public interface IBookingSlotService
{
    // Validate station existence, ownership, and configuration before adding a slot.
    Task<EnergyBookingSlotResponse> AddSlotAsync(
        string id,
        CreateBookingSlotRequest request,
        CancellationToken cancellationToken = default);

    // Validate ownership and active reservation constraints before changing slot details.
    Task<EnergyBookingSlotResponse> UpdateSlotAsync(
        string id,
        string slotId,
        UpdateBookingSlotRequest request,
        CancellationToken cancellationToken = default);

    // Return the slot only after checking that it belongs to the requested station.
    Task<EnergyBookingSlotResponse> GetSlotByIdAsync(
        string id, string slotId, CancellationToken cancellationToken = default);

    // Validate filters and return one page of slot responses.
    Task<PagedResult<EnergyBookingSlotResponse>> GetSlotsAsync(
        BookingSlotQuery query, CancellationToken cancellationToken = default);

    // Enforce legal domain transitions and any live reservation constraints server-side.
    Task<EnergyBookingSlotResponse> UpdateSlotStatusAsync(
        string id,
        string slotId,
        UpdateSlotStatusRequest request,
        CancellationToken cancellationToken = default);

    // Check ownership and live reservations before retiring a slot.
    Task RemoveSlotAsync(string id, string slotId, CancellationToken cancellationToken = default);
}
