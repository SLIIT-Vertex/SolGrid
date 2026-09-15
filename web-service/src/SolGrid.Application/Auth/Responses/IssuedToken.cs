/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: IssuedToken.cs
 * Description: Represents an issued authentication token.
 * Contributor: Bawanthi K D R
 */

namespace SolGrid.Application.Auth.Responses;

public sealed class IssuedToken
{
    public string AccessToken { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; init; }
}
