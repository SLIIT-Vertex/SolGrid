/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationDocument.cs
 * Description: Defines the MongoDB document shape for energy reservations.
 * Contributor: Dilshan Yapa S Y C T
 */

using MongoDB.Bson.Serialization.Attributes;
using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;

namespace SolGrid.Infrastructure.Persistence.MongoDb.Documents;

internal sealed class ReservationDocument
{
    [BsonId]
    public string Id { get; init; } = string.Empty;

    public string ProsumerId { get; init; } = string.Empty;

    public string StationId { get; init; } = string.Empty;

    public string BookingSlotId { get; init; } = string.Empty;

    public DateTime ScheduledAtUtc { get; init; }

    public ReservationStatus Status { get; init; }

    public bool IsActiveForBookingSlot { get; init; }

    public int Version { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public DateTime? ApprovedAtUtc { get; init; }

    public string? ApprovedBy { get; init; }

    public DateTime? RejectedAtUtc { get; init; }

    public string? RejectedBy { get; init; }

    public string? RejectionReason { get; init; }

    public DateTime? CancelledAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }

    public string? CompletedBy { get; init; }

    public string? QrVerificationTokenHash { get; init; }

    public DateTime? QrVerificationTokenIssuedAtUtc { get; init; }

    public DateTime? QrVerificationTokenExpiresAtUtc { get; init; }

    public DateTime? QrVerifiedAtUtc { get; init; }

    public static ReservationDocument FromDomain(EnergyReservation reservation)
    {
        // Convert a domain reservation into the MongoDB document shape.
        return new ReservationDocument
        {
            Id = reservation.Id,
            ProsumerId = reservation.ProsumerId,
            StationId = reservation.StationId,
            BookingSlotId = reservation.BookingSlotId,
            ScheduledAtUtc = reservation.ScheduledAt.UtcDateTime,
            Status = reservation.Status,
            IsActiveForBookingSlot = reservation.IsActive,
            Version = reservation.Version,
            CreatedAtUtc = reservation.CreatedAt.UtcDateTime,
            UpdatedAtUtc = reservation.UpdatedAt.UtcDateTime,
            ApprovedAtUtc = ToNullableUtcDateTime(reservation.ApprovedAt),
            ApprovedBy = reservation.ApprovedBy,
            RejectedAtUtc = ToNullableUtcDateTime(reservation.RejectedAt),
            RejectedBy = reservation.RejectedBy,
            RejectionReason = reservation.RejectionReason,
            CancelledAtUtc = ToNullableUtcDateTime(reservation.CancelledAt),
            CompletedAtUtc = ToNullableUtcDateTime(reservation.CompletedAt),
            CompletedBy = reservation.CompletedBy,
            QrVerificationTokenHash = reservation.QrVerificationTokenHash,
            QrVerificationTokenIssuedAtUtc = ToNullableUtcDateTime(reservation.QrVerificationTokenIssuedAt),
            QrVerificationTokenExpiresAtUtc = ToNullableUtcDateTime(reservation.QrVerificationTokenExpiresAt),
            QrVerifiedAtUtc = ToNullableUtcDateTime(reservation.QrVerifiedAt)
        };
    }

    public EnergyReservation ToDomain()
    {
        // Restore a domain reservation from the MongoDB document shape.
        return EnergyReservation.Restore(
            Id,
            ProsumerId,
            StationId,
            BookingSlotId,
            ToUtcOffset(ScheduledAtUtc),
            Status,
            ToUtcOffset(CreatedAtUtc),
            ToUtcOffset(UpdatedAtUtc),
            ToNullableUtcOffset(ApprovedAtUtc),
            ApprovedBy,
            ToNullableUtcOffset(RejectedAtUtc),
            RejectedBy,
            RejectionReason,
            ToNullableUtcOffset(CancelledAtUtc),
            ToNullableUtcOffset(CompletedAtUtc),
            CompletedBy,
            QrVerificationTokenHash,
            ToNullableUtcOffset(QrVerificationTokenIssuedAtUtc),
            ToNullableUtcOffset(QrVerificationTokenExpiresAtUtc),
            ToNullableUtcOffset(QrVerifiedAtUtc),
            Version);
    }

    private static DateTime? ToNullableUtcDateTime(DateTimeOffset? value)
    {
        // Convert optional domain timestamps into UTC persistence values.
        return value?.UtcDateTime;
    }

    private static DateTimeOffset? ToNullableUtcOffset(DateTime? value)
    {
        // Convert optional persisted timestamps to explicit UTC offsets.
        return value.HasValue ? ToUtcOffset(value.Value) : null;
    }

    private static DateTimeOffset ToUtcOffset(DateTime value)
    {
        // Convert persisted timestamps to explicit UTC offsets.
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
