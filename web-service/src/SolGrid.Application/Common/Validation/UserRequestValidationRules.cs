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
    public const int NameMaximumLength = 100;

    public const int EmailMaximumLength = 254;

    public const int PasswordMinimumLength = 8;

    public const int PasswordMaximumLength = 128;

    public const int SearchTextMaximumLength = 100;

    public const int PageSizeMaximum = 100;

    public static bool HasValue(string value)
    {
        // Check whether a required text value has meaningful content.
        return !string.IsNullOrWhiteSpace(value);
    }

    public static bool HasEmailShape(string email)
    {
        // Apply a practical email shape check without attempting full mailbox verification.
        if (!HasValue(email))
        {
            return false;
        }

        var normalizedEmail = email.Trim();
        if (normalizedEmail.Length > EmailMaximumLength || normalizedEmail.Any(char.IsWhiteSpace))
        {
            return false;
        }

        var atIndex = normalizedEmail.IndexOf('@');
        var domainDotIndex = normalizedEmail.IndexOf('.', atIndex + 2);
        return atIndex > 0
            && atIndex == normalizedEmail.LastIndexOf('@')
            && atIndex < normalizedEmail.Length - 3
            && domainDotIndex > atIndex + 1
            && domainDotIndex < normalizedEmail.Length - 1;
    }
}
