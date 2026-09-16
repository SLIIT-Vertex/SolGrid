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
    public static ReservationResponse ToResponse(EnergyReservation reservation)
    {
        // Map a reservation without exposing QR token hash details.
        return new ReservationResponse
        {
            Id = reservation.Id,
            ProsumerId = reservation.ProsumerId,
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
