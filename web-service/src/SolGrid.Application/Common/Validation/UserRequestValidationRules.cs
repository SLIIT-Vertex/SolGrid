/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: UserRequestValidationRules.cs
 * Description: Provides shared validation helpers for web user requests.
 * Contributor: Bawanthi K D R
 */

namespace SolGrid.Application.Common.Validation;

public static class UserRequestValidationRules
{
    public static bool HasValue(string value)
    {
        // Check whether a required text value has meaningful content.
        return !string.IsNullOrWhiteSpace(value);
    }

    public static bool HasEmailShape(string email)
    {
        // Check for a minimal email shape before application services process the request.
        return HasValue(email) && email.Contains('@', StringComparison.Ordinal);
    }
}
