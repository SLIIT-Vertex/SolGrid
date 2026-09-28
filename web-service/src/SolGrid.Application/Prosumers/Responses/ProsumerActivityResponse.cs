/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ProsumerActivityResponse.cs
 * Description: Prosumer account lifecycle and history contracts.
 * Contributor: Gunasekara H N
 */

using SolGrid.Domain.Enums;

namespace SolGrid.Application.Prosumers.Responses;

public sealed record ProsumerActivityResponse(
    string Action, string? ActorId, string ActorRole, string? Reason,
    ProsumerAccountStatus Status, DateTimeOffset OccurredAt, long Version);
