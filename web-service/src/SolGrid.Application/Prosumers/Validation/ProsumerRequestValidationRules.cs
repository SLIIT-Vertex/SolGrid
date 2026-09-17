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
    public const int PhoneNumberLength = 10;

    public static bool HasNicValue(string nic)
    {
        // Require a NIC value without imposing an undocumented national-format policy.
        return UserRequestValidationRules.HasValue(nic);
    }

    public static bool HasSupportedSriLankanNicFormat(string nic)
    {
        // Accept the commonly used legacy and current Sri Lankan NIC shapes.
        if (!HasNicValue(nic))
        {
            return false;
        }

        var normalizedNic = nic.Trim();
        return (normalizedNic.Length == 10
                && normalizedNic[..9].All(char.IsDigit)
                && normalizedNic[9] is 'V' or 'v' or 'X' or 'x')
            || (normalizedNic.Length == 12 && normalizedNic.All(char.IsDigit));
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
        return normalizedPhoneNumber.Length == PhoneNumberLength
            && normalizedPhoneNumber[0] == '0'
            && normalizedPhoneNumber.All(char.IsDigit);
    }
}
