/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationResponse.cs
 * Description: Returns energy reservation details to API clients.
 * Contributor: Dilshan Yapa S Y C T
 */

using SolGrid.Domain.Enums;

namespace SolGrid.Application.Reservations.Responses;

public sealed class ReservationResponse
{
    public string Id { get; init; } = string.Empty;

    public string ReferenceCode { get; init; } = string.Empty;

    public string ProsumerId { get; init; } = string.Empty;

    /// <summary>Prosumer NIC — populated on single-reservation reads so operators can confirm identity.</summary>
    public string? ProsumerNic { get; init; }

    /// <summary>Prosumer full name — populated on single-reservation reads for operator confirmation.</summary>
    public string? ProsumerName { get; init; }

    public string StationId { get; init; } = string.Empty;

    public string BookingSlotId { get; init; } = string.Empty;

    public DateTimeOffset ScheduledAt { get; init; }

    public ReservationStatus Status { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public DateTimeOffset? ApprovedAt { get; init; }

    public string? ApprovedBy { get; init; }

    public DateTimeOffset? RejectedAt { get; init; }

    public string? RejectedBy { get; init; }

    public string? RejectionReason { get; init; }

    public DateTimeOffset? CancelledAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public string? CompletedBy { get; init; }

    public bool HasQrVerificationToken { get; init; }

    public DateTimeOffset? QrVerificationTokenIssuedAt { get; init; }

    public DateTimeOffset? QrVerificationTokenExpiresAt { get; init; }

    public DateTimeOffset? QrVerifiedAt { get; init; }
}
