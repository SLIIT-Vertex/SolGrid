/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: JwtOptions.cs
 * Description: Holds typed JWT configuration for SolGrid authentication.
 * Contributor: Bawanthi K D R
 */

namespace SolGrid.Infrastructure.Security;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    public int ExpiresMinutes { get; init; } = 60;
}
