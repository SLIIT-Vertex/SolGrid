/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: UpdateUserRequest.cs
 * Description: Carries editable web user account fields.
 * Contributor: Bawanthi K D R
 */

using SolGrid.Domain.Enums;

namespace SolGrid.Application.Users.Requests;

public sealed class UpdateUserRequest
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public UserRole Role { get; init; }
}
