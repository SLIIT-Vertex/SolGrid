/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IBookingSlotRepository.cs
 * Description: Defines persistence-neutral booking slot access for microgrid application services.
 * Contributor: Kavishi Godage
 */

using SolGrid.Application.Common.Models;
using SolGrid.Domain.Entities;

namespace SolGrid.Application.SolarStations.Interfaces;

public interface IBookingSlotRepository
{
    // Return null when the globally identified slot does not exist.
    Task<EnergyBookingSlot?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    // Query slot state using the common pagination contract.
    Task<PagedResult<EnergyBookingSlot>> GetPagedAsync(BookingSlotQuery query, CancellationToken cancellationToken = default);

    // Persist a slot after the service has validated its owning station.
    Task AddAsync(EnergyBookingSlot slot, CancellationToken cancellationToken = default);

    // Persist a validated slot mutation without accepting persistence-specific values.
    Task UpdateAsync(EnergyBookingSlot slot, CancellationToken cancellationToken = default);

    // Remove a slot only after the service has checked station ownership and live reservations.
    Task RemoveAsync(string id, CancellationToken cancellationToken = default);
}
