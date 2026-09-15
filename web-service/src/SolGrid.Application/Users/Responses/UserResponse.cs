/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: UserResponse.cs
 * Description: Returns safe web user account details to API clients.
 * Contributor: Bawanthi K D R
 */

using SolGrid.Domain.Enums;

namespace SolGrid.Application.Users.Responses;

public sealed class UserResponse
{
    public string Id { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    public AccountStatus Status { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
