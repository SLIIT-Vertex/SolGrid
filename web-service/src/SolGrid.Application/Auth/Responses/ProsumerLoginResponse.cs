/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ProsumerLoginResponse.cs
 * Description: Returns a prosumer access token and safe profile details.
 * Contributor: Gunasekara H N
 */

using SolGrid.Application.Prosumers.Responses;

namespace SolGrid.Application.Auth.Responses;

public sealed class ProsumerLoginResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; init; }

    public ProsumerResponse Prosumer { get; init; } = new();
}
