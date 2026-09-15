/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: LoginRequest.cs
 * Description: Carries web user login credentials.
 * Contributor: Bawanthi K D R
 */

namespace SolGrid.Application.Auth.Requests;

public sealed class LoginRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
