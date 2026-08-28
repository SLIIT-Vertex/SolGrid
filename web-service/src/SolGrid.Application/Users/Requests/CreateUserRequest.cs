/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: CreateUserRequest.cs
 * Description: Carries data required to create a web user account.
 * Contributor: Bawanthi K D R
 */

using SolGrid.Domain.Enums;

namespace SolGrid.Application.Users.Requests;

public sealed class CreateUserRequest
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public UserRole Role { get; init; }
}
