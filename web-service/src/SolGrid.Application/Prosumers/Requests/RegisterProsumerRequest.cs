/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: RegisterProsumerRequest.cs
 * Description: Carries data required to register a solar prosumer profile.
 * Contributor: Gunasekara H N
 */

namespace SolGrid.Application.Prosumers.Requests;

public sealed class RegisterProsumerRequest
{
    public string Nic { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }

    public string Password { get; init; } = string.Empty;
}
