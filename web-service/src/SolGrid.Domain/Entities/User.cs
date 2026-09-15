/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: User.cs
 * Description: Represents a web user account for backoffice and grid operator access.
 * Contributor: Bawanthi K D R
 */

using SolGrid.Domain.Enums;

namespace SolGrid.Domain.Entities;

public sealed class User
{
    private User(
        string id,
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        UserRole role,
        AccountStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        // Initialize a validated user instance for creation or persistence rehydration.
        Id = RequireValue(id, nameof(id));
        FirstName = RequireValue(firstName, nameof(firstName));
        LastName = RequireValue(lastName, nameof(lastName));
        Email = NormalizeEmail(email);
        PasswordHash = RequireValue(passwordHash, nameof(passwordHash));
        Role = RequireDefinedEnum(role, nameof(role));
        Status = RequireDefinedEnum(status, nameof(status));
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public string Id { get; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    public AccountStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsActive => Status == AccountStatus.Active;

    public static User Create(
        string id,
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        UserRole role,
        DateTimeOffset createdAt)
    {
        // Create a new active web user with validated identity and credential state.
        return new User(
            id,
            firstName,
            lastName,
            email,
            passwordHash,
            role,
            AccountStatus.Active,
            createdAt,
            createdAt);
    }

    public static User Restore(
        string id,
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        UserRole role,
        AccountStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        // Rehydrate a user from persistence without exposing persistence-specific types.
        return new User(id, firstName, lastName, email, passwordHash, role, status, createdAt, updatedAt);
    }

    public void UpdateProfile(string firstName, string lastName, string email, DateTimeOffset updatedAt)
    {
        // Update editable profile fields while preserving invariant checks.
        FirstName = RequireValue(firstName, nameof(firstName));
        LastName = RequireValue(lastName, nameof(lastName));
        Email = NormalizeEmail(email);
        MarkUpdated(updatedAt);
    }

    public void ChangeRole(UserRole role, DateTimeOffset updatedAt)
    {
        // Change the user's role after the application service has authorized the action.
        Role = RequireDefinedEnum(role, nameof(role));
        MarkUpdated(updatedAt);
    }

    public void ChangePasswordHash(string passwordHash, DateTimeOffset updatedAt)
    {
        // Replace the stored password hash without ever accepting plaintext for storage.
        PasswordHash = RequireValue(passwordHash, nameof(passwordHash));
        MarkUpdated(updatedAt);
    }

    public void Activate(DateTimeOffset updatedAt)
    {
        // Mark the account as active.
        Status = AccountStatus.Active;
        MarkUpdated(updatedAt);
    }

    public void Deactivate(DateTimeOffset updatedAt)
    {
        // Mark the account as inactive.
        Status = AccountStatus.Inactive;
        MarkUpdated(updatedAt);
    }

    private static string RequireValue(string value, string parameterName)
    {
        // Reject missing text values before they enter the domain model.
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    private static string NormalizeEmail(string email)
    {
        // Normalize email for consistent uniqueness checks and authentication lookups.
        var normalizedEmail = RequireValue(email, nameof(email)).ToLowerInvariant();
        if (!normalizedEmail.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("A valid email address is required.", nameof(email));
        }

        return normalizedEmail;
    }

    private static TEnum RequireDefinedEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        // Reject undefined enum values before they become domain state.
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value is not supported.");
        }

        return value;
    }

    private void MarkUpdated(DateTimeOffset updatedAt)
    {
        // Capture the latest domain mutation timestamp.
        UpdatedAt = updatedAt;
    }
}
