/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ApplicationExceptionBase.cs
 * Description: Provides a base exception type for application-layer failures.
 * Contributor: Bawanthi K D R
 */

namespace SolGrid.Application.Common.Exceptions;

public abstract class ApplicationExceptionBase : Exception
{
    protected ApplicationExceptionBase(string message)
        : base(message)
    {
        // Store a safe application error message for API translation.
    }
}
