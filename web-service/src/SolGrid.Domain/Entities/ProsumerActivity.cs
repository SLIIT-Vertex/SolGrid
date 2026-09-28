/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ProsumerActivity.cs
 * Description: Records a server-authored account event without profile or credential snapshots.
 * Contributor: Gunasekara H N
 */

using SolGrid.Domain.Enums;

namespace SolGrid.Domain.Entities;

public sealed record ProsumerActivity(
    string Action,
    string ActorId,
    string ActorRole,
    string? Reason,
    ProsumerAccountStatus Status,
    DateTimeOffset OccurredAt,
    long Version);
