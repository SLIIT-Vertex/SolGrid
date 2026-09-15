/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: AccountInactiveException.cs
 * Description: Represents login attempts for inactive user accounts.
 * Contributor: Bawanthi K D R
 */

namespace SolGrid.Application.Common.Exceptions;

public sealed class AccountInactiveException : ApplicationExceptionBase
{
    public AccountInactiveException()
        : base("User account is inactive.")
    {
        // Create a consistent inactive account authentication failure.
    }
}
