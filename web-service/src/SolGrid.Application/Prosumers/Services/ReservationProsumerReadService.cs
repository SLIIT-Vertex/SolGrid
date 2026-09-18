/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationProsumerReadService.cs
 * Description: Adapts prosumer data to the reservation component's focused read contract.
 * Contributor: Gunasekara H N
 */

using SolGrid.Application.Prosumers.Interfaces;
using SolGrid.Application.Prosumers.Responses;
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
        // Read the linked profile through the reservation contract, using the safe response mapper.
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
                FullName = $"{prosumer.FirstName} {prosumer.LastName}".Trim(),
                Details = ProsumerResponseMapper.ToResponse(prosumer)
            };
    }
}
