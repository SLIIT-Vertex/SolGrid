/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: AuthenticationFailedException.cs
 * Description: Represents failed authentication attempts.
 * Contributor: Bawanthi K D R
 */

namespace SolGrid.Application.Common.Exceptions;

public sealed class AuthenticationFailedException : ApplicationExceptionBase
{
    public AuthenticationFailedException()
        : base("Invalid email or password.")
    {
        // Create a consistent authentication failure without revealing which credential failed.
    }
}
