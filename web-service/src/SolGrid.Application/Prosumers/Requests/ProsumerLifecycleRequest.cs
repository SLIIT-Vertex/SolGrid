/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ProsumerLifecycleRequest.cs
 * Description: Prosumer account lifecycle and history contracts.
 * Contributor: Gunasekara H N
 */

namespace SolGrid.Application.Prosumers.Requests;

public sealed class ProsumerLifecycleRequest
{
    public long? ExpectedVersion { get; init; }
    public string Reason { get; init; } = string.Empty;
}
