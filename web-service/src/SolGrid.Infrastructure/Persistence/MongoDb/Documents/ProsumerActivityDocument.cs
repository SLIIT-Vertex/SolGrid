/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ProsumerActivityDocument.cs
 * Description: Prosumer account lifecycle and history contracts.
 * Contributor: Gunasekara H N
 */

using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;

namespace SolGrid.Infrastructure.Persistence.MongoDb.Documents;

internal sealed class ProsumerActivityDocument
{
    public string Action { get; init; } = string.Empty;
    public string ActorId { get; init; } = string.Empty;
    public string ActorRole { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public ProsumerAccountStatus Status { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public long Version { get; init; }

    public static ProsumerActivityDocument FromDomain(ProsumerActivity item)
    {
        // Store history timestamps as UTC BSON dates.
        return new() { Action = item.Action, ActorId = item.ActorId, ActorRole = item.ActorRole,
            Reason = item.Reason, Status = item.Status, OccurredAtUtc = item.OccurredAt.UtcDateTime, Version = item.Version };
    }

    public ProsumerActivity ToDomain()
    {
        // Restore immutable events without changing their recorded order or identity.
        return new(Action, ActorId, ActorRole, Reason, Status,
            new DateTimeOffset(DateTime.SpecifyKind(OccurredAtUtc, DateTimeKind.Utc)), Version);
    }
}
