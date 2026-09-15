/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ConflictException.cs
 * Description: Represents application conflicts such as duplicate unique fields.
 * Contributor: Bawanthi K D R
 */

namespace SolGrid.Application.Common.Exceptions;

public sealed class ConflictException : ApplicationExceptionBase
{
    public ConflictException(string message)
        : base(message)
    {
        // Create a conflict exception with a client-safe message.
    }
}
