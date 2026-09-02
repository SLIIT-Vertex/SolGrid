/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ValidationException.cs
 * Description: Represents application validation failures.
 * Contributor: Bawanthi K D R
 */

namespace SolGrid.Application.Common.Exceptions;

public sealed class ValidationException : ApplicationExceptionBase
{
    public ValidationException(IEnumerable<string> errors)
        : base("Validation failed.")
    {
        // Capture validation errors for API problem responses.
        Errors = errors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Select(error => error.Trim())
            .ToArray();
    }

    public IReadOnlyList<string> Errors { get; }
}
