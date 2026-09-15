/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: UserDocument.cs
 * Description: Defines the MongoDB document shape for web user accounts.
 * Contributor: Bawanthi K D R
 */

using MongoDB.Bson.Serialization.Attributes;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;

namespace SolGrid.Infrastructure.Persistence.MongoDb.Documents;

internal sealed class UserDocument
{
    [BsonId]
    public string Id { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string PasswordHash { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    public AccountStatus AccountStatus { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public static UserDocument FromDomain(User user)
    {
        // Convert a domain user into the MongoDB document shape.
        return new UserDocument
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            Role = user.Role,
            AccountStatus = user.Status,
            CreatedAtUtc = user.CreatedAt.UtcDateTime,
            UpdatedAtUtc = user.UpdatedAt.UtcDateTime
        };
    }

    public User ToDomain()
    {
        // Restore a domain user from the MongoDB document shape.
        return User.Restore(
            Id,
            FirstName,
            LastName,
            Email,
            PasswordHash,
            Role,
            AccountStatus,
            ToUtcOffset(CreatedAtUtc),
            ToUtcOffset(UpdatedAtUtc));
    }

    private static DateTimeOffset ToUtcOffset(DateTime value)
    {
        // Convert persisted timestamps to explicit UTC offsets.
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
