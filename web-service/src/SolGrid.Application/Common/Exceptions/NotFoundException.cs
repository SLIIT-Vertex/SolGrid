/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: NotFoundException.cs
 * Description: Represents a missing application resource.
 * Contributor: Bawanthi K D R
 */

namespace SolGrid.Application.Common.Exceptions;

public sealed class NotFoundException : ApplicationExceptionBase
{
    public NotFoundException(string resourceName, string id)
        : base($"{resourceName} '{id}' was not found.")
    {
        // Create a not-found exception with resource context.
    }
}
