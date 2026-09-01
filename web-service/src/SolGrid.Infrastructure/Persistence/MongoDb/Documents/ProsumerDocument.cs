/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ProsumerDocument.cs
 * Description: Defines the MongoDB document shape for solar prosumer profiles.
 * Contributor: Gunasekara H N
 */

using MongoDB.Bson.Serialization.Attributes;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;

namespace SolGrid.Infrastructure.Persistence.MongoDb.Documents;

internal sealed class ProsumerDocument
{
    [BsonId]
    public string Nic { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }

    public string PasswordHash { get; init; } = string.Empty;

    public ProsumerAccountStatus AccountStatus { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public static ProsumerDocument FromDomain(Prosumer prosumer)
    {
        // Convert a domain prosumer into the MongoDB document shape.
        return new ProsumerDocument
        {
            Nic = prosumer.Nic,
            FirstName = prosumer.FirstName,
            LastName = prosumer.LastName,
            Email = prosumer.Email,
            PhoneNumber = prosumer.PhoneNumber,
            PasswordHash = prosumer.PasswordHash,
            AccountStatus = prosumer.Status,
            CreatedAtUtc = prosumer.CreatedAt.UtcDateTime,
            UpdatedAtUtc = prosumer.UpdatedAt.UtcDateTime
        };
    }

    public Prosumer ToDomain()
    {
        // Restore a domain prosumer from the MongoDB document shape.
        return Prosumer.Restore(
            Nic,
            FirstName,
            LastName,
            Email,
            PhoneNumber,
            PasswordHash,
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
