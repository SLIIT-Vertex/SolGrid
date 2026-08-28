/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ProsumerRequestValidationRules.cs
 * Description: Provides shared validation helpers for prosumer request contracts.
 * Contributor: Gunasekara H N
 */

using SolGrid.Application.Common.Validation;

namespace SolGrid.Application.Prosumers.Validation;

public static class ProsumerRequestValidationRules
{
    public static bool HasNicValue(string nic)
    {
        // Require a NIC value without imposing an undocumented national-format policy.
        return UserRequestValidationRules.HasValue(nic);
    }

    public static bool HasEmailShape(string email)
    {
        // Reuse the established minimal email validation convention.
        return UserRequestValidationRules.HasEmailShape(email);
    }

    public static bool HasPhoneNumberShape(string? phoneNumber)
    {
        // Accept an omitted phone number or a simple human-entered phone contact format.
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return true;
        }

        var normalizedPhoneNumber = phoneNumber.Trim();
        return normalizedPhoneNumber.Length is >= 7 and <= 20
            && normalizedPhoneNumber.All(character => char.IsDigit(character)
                || character is ' ' or '+' or '-' or '(' or ')');
    }
}
