/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: Prosumer.cs
 * Description: Represents a solar prosumer profile and account lifecycle.
 * Contributor: Gunasekara H N
 */

using SolGrid.Domain.Enums;

namespace SolGrid.Domain.Entities;

public sealed class Prosumer
{
    private Prosumer(
        string nic,
        string firstName,
        string lastName,
        string email,
        string? phoneNumber,
        string passwordHash,
        ProsumerAccountStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        // Initialize a validated prosumer profile for creation or persistence rehydration.
        Nic = NormalizeNic(nic);
        FirstName = RequireValue(firstName, nameof(firstName));
        LastName = RequireValue(lastName, nameof(lastName));
        Email = NormalizeEmail(email);
        PhoneNumber = NormalizeOptionalPhoneNumber(phoneNumber);
        PasswordHash = RequireValue(passwordHash, nameof(passwordHash));
        Status = RequireDefinedEnum(status, nameof(status));
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public string Nic { get; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string Email { get; private set; }

    public string? PhoneNumber { get; private set; }

    public string PasswordHash { get; private set; }

    public ProsumerAccountStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsActive => Status == ProsumerAccountStatus.Active;

    public static Prosumer Create(
        string nic,
        string firstName,
        string lastName,
        string email,
        string? phoneNumber,
        string passwordHash,
        DateTimeOffset createdAt)
    {
        // Create a pending prosumer profile that awaits Backoffice activation.
        return new Prosumer(
            nic,
            firstName,
            lastName,
            email,
            phoneNumber,
            passwordHash,
            ProsumerAccountStatus.Pending,
            createdAt,
            createdAt);
    }

    public static Prosumer Restore(
        string nic,
        string firstName,
        string lastName,
        string email,
        string? phoneNumber,
        string passwordHash,
        ProsumerAccountStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        // Rehydrate a prosumer without exposing persistence-specific types to Domain.
        return new Prosumer(nic, firstName, lastName, email, phoneNumber, passwordHash, status, createdAt, updatedAt);
    }

    public void UpdateProfile(
        string firstName,
        string lastName,
        string email,
        string? phoneNumber,
        DateTimeOffset updatedAt)
    {
        // Update only editable profile fields while preserving the immutable NIC.
        FirstName = RequireValue(firstName, nameof(firstName));
        LastName = RequireValue(lastName, nameof(lastName));
        Email = NormalizeEmail(email);
        PhoneNumber = NormalizeOptionalPhoneNumber(phoneNumber);
        MarkUpdated(updatedAt);
    }

    public void Activate(DateTimeOffset updatedAt)
    {
        // Activate a newly registered pending prosumer account.
        EnsureStatus([ProsumerAccountStatus.Pending], "Only pending prosumer accounts can be activated.");
        Status = ProsumerAccountStatus.Active;
        MarkUpdated(updatedAt);
    }

    public void RequestDeactivation(DateTimeOffset updatedAt)
    {
        // Record an active prosumer's request for account deactivation.
        EnsureStatus([ProsumerAccountStatus.Active], "Only active prosumer accounts can request deactivation.");
        Status = ProsumerAccountStatus.DeactivationRequested;
        MarkUpdated(updatedAt);
    }

    public void Deactivate(DateTimeOffset updatedAt)
    {
        // Deactivate an account while retaining its historical profile record.
        EnsureStatus(
            [
                ProsumerAccountStatus.Pending,
                ProsumerAccountStatus.Active,
                ProsumerAccountStatus.DeactivationRequested
            ],
            "Prosumer account cannot be deactivated from its current status.");
        Status = ProsumerAccountStatus.Deactivated;
        MarkUpdated(updatedAt);
    }

    public void Reactivate(DateTimeOffset updatedAt)
    {
        // Restore a deactivated prosumer account to active status.
        EnsureStatus([ProsumerAccountStatus.Deactivated], "Only deactivated prosumer accounts can be reactivated.");
        Status = ProsumerAccountStatus.Active;
        MarkUpdated(updatedAt);
    }

    private static string RequireValue(string value, string parameterName)
    {
        // Reject missing text values before they enter prosumer domain state.
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    private static string NormalizeNic(string nic)
    {
        // Normalize the immutable assignment-required NIC business identifier.
        return RequireValue(nic, nameof(nic)).ToUpperInvariant();
    }

    private static string NormalizeEmail(string email)
    {
        // Normalize email for consistent uniqueness checks without persistence coupling.
        var normalizedEmail = RequireValue(email, nameof(email)).ToLowerInvariant();
        if (!normalizedEmail.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("A valid email address is required.", nameof(email));
        }

        return normalizedEmail;
    }

    private static string? NormalizeOptionalPhoneNumber(string? phoneNumber)
    {
        // Normalize optional contact information without turning missing input into content.
        return string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
    }

    private static TEnum RequireDefinedEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        // Reject undefined lifecycle values before they become prosumer state.
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value is not supported.");
        }

        return value;
    }

    private void EnsureStatus(IReadOnlyCollection<ProsumerAccountStatus> allowedStatuses, string message)
    {
        // Reject account lifecycle transitions that are not legal from the current state.
        if (!allowedStatuses.Contains(Status))
        {
            throw new InvalidOperationException(message);
        }
    }

    private void MarkUpdated(DateTimeOffset updatedAt)
    {
        // Capture the latest prosumer profile or lifecycle mutation timestamp.
        UpdatedAt = updatedAt;
    }
}
