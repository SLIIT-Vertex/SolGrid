/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: UpdateProsumerRequest.cs
 * Description: Carries editable solar prosumer profile fields.
 * Contributor: Gunasekara H N
 */

namespace SolGrid.Application.Prosumers.Requests;

public sealed class UpdateProsumerRequest
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }
}
