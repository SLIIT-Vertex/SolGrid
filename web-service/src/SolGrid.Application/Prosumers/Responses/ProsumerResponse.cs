/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ProsumerResponse.cs
 * Description: Returns safe solar prosumer profile and lifecycle details to API clients.
 * Contributor: Gunasekara H N
 */

using SolGrid.Domain.Enums;

namespace SolGrid.Application.Prosumers.Responses;

public sealed class ProsumerResponse
{
    public string Nic { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }

    public ProsumerAccountStatus Status { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
