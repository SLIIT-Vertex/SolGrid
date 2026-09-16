/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IProsumerService.cs
 * Description: Defines prosumer profile and account lifecycle use cases.
 * Contributor: Gunasekara H N
 */

using SolGrid.Application.Common.Models;
using SolGrid.Application.Prosumers.Requests;
using SolGrid.Application.Prosumers.Responses;
using SolGrid.Application.Users.Interfaces;

namespace SolGrid.Application.Prosumers.Interfaces;

public interface IProsumerService
{
    Task<ProsumerResponse> RegisterProsumerAsync(RegisterProsumerRequest request, CancellationToken cancellationToken = default);

    Task<ProsumerResponse> GetMyProsumerAsync(CancellationToken cancellationToken = default);

    Task<ProsumerResponse> UpdateMyProsumerAsync(UpdateProsumerRequest request, CancellationToken cancellationToken = default);

    Task RequestMyDeactivationAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<ProsumerResponse>> GetProsumersAsync(ProsumerQuery query, CancellationToken cancellationToken = default);

    Task<ProsumerResponse> GetProsumerByNicAsync(string nic, CancellationToken cancellationToken = default);

    Task ActivateProsumerAsync(string nic, CancellationToken cancellationToken = default);

    Task DeactivateProsumerAsync(string nic, CancellationToken cancellationToken = default);

    Task ReactivateProsumerAsync(string nic, CancellationToken cancellationToken = default);
}
