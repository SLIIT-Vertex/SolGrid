/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationProsumerReadService.cs
 * Description: Adapts prosumer data to the reservation component's focused read contract.
 * Contributor: Gunasekara H N
 */

using SolGrid.Application.Prosumers.Interfaces;
using SolGrid.Application.Reservations.Interfaces;

namespace SolGrid.Application.Prosumers.Services;

public sealed class ReservationProsumerReadService : IReservationProsumerReadService
{
    private readonly IProsumerRepository prosumerRepository;

    public ReservationProsumerReadService(IProsumerRepository prosumerRepository)
    {
        // Capture the prosumer abstraction without exposing persistence implementation details.
        this.prosumerRepository = prosumerRepository;
    }

    public async Task<ReservationProsumerSnapshot?> GetByIdAsync(
        string prosumerId,
        CancellationToken cancellationToken = default)
    {
        // Return only the stable NIC reference and booking-eligibility state required by reservations.
        if (string.IsNullOrWhiteSpace(prosumerId))
        {
            return null;
        }

        var prosumer = await prosumerRepository
            .GetByNicAsync(prosumerId.Trim(), cancellationToken)
            .ConfigureAwait(false);

        return prosumer is null
            ? null
            : new ReservationProsumerSnapshot
            {
                Id = prosumer.Nic,
                IsActive = prosumer.IsActive,
                Nic = prosumer.Nic,
                FullName = $"{prosumer.FirstName} {prosumer.LastName}".Trim()
            };
    }
}
