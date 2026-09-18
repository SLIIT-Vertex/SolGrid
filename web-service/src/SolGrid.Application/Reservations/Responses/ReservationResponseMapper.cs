/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationResponseMapper.cs
 * Description: Maps reservation domain entities to response DTOs.
 * Contributor: Dilshan Yapa S Y C T
 */

using SolGrid.Domain.Entities;

namespace SolGrid.Application.Reservations.Responses;

public static class ReservationResponseMapper
{
    public static ReservationResponse ToResponse(
        EnergyReservation reservation,
        string? prosumerNic = null,
        string? prosumerName = null)
    {
        // Map a reservation without exposing QR token hash details. Prosumer identity fields are
        // optional and only populated on single-reservation reads where the operator needs them.
        return new ReservationResponse
        {
            Id = reservation.Id,
            ReferenceCode = reservation.ReferenceCode,
            ProsumerId = reservation.ProsumerId,
            ProsumerNic = prosumerNic,
            ProsumerName = prosumerName,
            StationId = reservation.StationId,
            BookingSlotId = reservation.BookingSlotId,
            ScheduledAt = reservation.ScheduledAt,
            Status = reservation.Status,
            CreatedAt = reservation.CreatedAt,
            UpdatedAt = reservation.UpdatedAt,
            ApprovedAt = reservation.ApprovedAt,
            ApprovedBy = reservation.ApprovedBy,
            RejectedAt = reservation.RejectedAt,
            RejectedBy = reservation.RejectedBy,
            RejectionReason = reservation.RejectionReason,
            CancelledAt = reservation.CancelledAt,
            CompletedAt = reservation.CompletedAt,
            CompletedBy = reservation.CompletedBy,
            HasQrVerificationToken = !string.IsNullOrWhiteSpace(reservation.QrVerificationTokenHash),
            QrVerificationTokenIssuedAt = reservation.QrVerificationTokenIssuedAt,
            QrVerificationTokenExpiresAt = reservation.QrVerificationTokenExpiresAt,
            QrVerifiedAt = reservation.QrVerifiedAt
        };
    }
}
