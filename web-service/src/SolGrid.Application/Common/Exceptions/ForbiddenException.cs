/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ForbiddenException.cs
 * Description: Represents an authenticated caller lacking permission for an application action.
 * Contributor: Dilshan Yapa S Y C T
 */

namespace SolGrid.Application.Common.Exceptions;

public sealed class ForbiddenException : ApplicationExceptionBase
{
    public ForbiddenException(string message)
        : base(message)
    {
        // Create a forbidden exception with a client-safe message.
    }
}
