/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ValidationResult.cs
 * Description: Represents the outcome of application request validation.
 * Contributor: Bawanthi K D R
 */

namespace SolGrid.Application.Common.Validation;

public sealed class ValidationResult
{
    private ValidationResult(IReadOnlyList<string> errors)
    {
        // Store validation errors in an immutable shape for callers.
        Errors = errors;
    }

    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<string> Errors { get; }

    public static ValidationResult Success()
    {
        // Create a successful validation result.
        return new ValidationResult(Array.Empty<string>());
    }

    public static ValidationResult Failure(IEnumerable<string> errors)
    {
        // Create a failed validation result from the supplied errors.
        var errorList = errors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Select(error => error.Trim())
            .ToArray();

        return new ValidationResult(errorList);
    }
}
