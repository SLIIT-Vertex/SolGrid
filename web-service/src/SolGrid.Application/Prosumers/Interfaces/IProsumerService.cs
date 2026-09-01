/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IProsumerService.cs
 * Description: Defines prosumer profile and account lifecycle use cases.
 * Contributor: Gunasekara H N
 */

using SolGrid.Application.Prosumers.Requests;
using SolGrid.Application.Prosumers.Responses;

namespace SolGrid.Application.Prosumers.Interfaces;

public interface IProsumerService
{
    Task<ProsumerResponse> RegisterProsumerAsync(RegisterProsumerRequest request, CancellationToken cancellationToken = default);
}
