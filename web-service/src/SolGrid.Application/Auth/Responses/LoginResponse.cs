/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: LoginResponse.cs
 * Description: Returns an authenticated web user and issued access token.
 * Contributor: Bawanthi K D R
 */

using SolGrid.Application.Users.Responses;

namespace SolGrid.Application.Auth.Responses;

public sealed class LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; init; }

    public UserResponse User { get; init; } = new();
}
