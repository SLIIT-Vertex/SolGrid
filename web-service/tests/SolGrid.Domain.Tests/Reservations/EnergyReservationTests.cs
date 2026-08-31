/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: EnergyReservationTests.cs
 * Description: Verifies energy reservation domain invariants and status transitions.
 * Contributor: Dilshan Yapa S Y C T
 */

using SolGrid.Domain.Entities;
using SolGrid.Domain.Enums;
using Xunit;

namespace SolGrid.Domain.Tests.Reservations;

public sealed class EnergyReservationTests
{
    [Fact]
    public void Create_WithRequiredReferences_CreatesPendingReservation()
    {
        // Verify a newly created reservation starts as pending and active.
        var now = DateTimeOffset.UtcNow;
        var reservation = CreateReservation(now);

        Assert.Equal(ReservationStatus.Pending, reservation.Status);
        Assert.True(reservation.IsActive);
        Assert.Equal(now, reservation.CreatedAt);
        Assert.Equal(now, reservation.UpdatedAt);
    }

    [Fact]
    public void Create_WithMissingProsumerId_ThrowsArgumentException()
    {
        // Verify required reservation references cannot be blank.
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() => EnergyReservation.Create(
            "reservation-1",
            " ",
            "station-1",
            "slot-1",
            now.AddHours(2),
            now));
    }

    [Fact]
    public void Approve_FromPending_ChangesStatusAndStoresMetadata()
    {
        // Verify pending reservations can be approved once.
        var now = DateTimeOffset.UtcNow;
        var reservation = CreateReservation(now);

        reservation.Approve("backoffice-1", now.AddMinutes(5));

        Assert.Equal(ReservationStatus.Approved, reservation.Status);
        Assert.Equal("backoffice-1", reservation.ApprovedBy);
        Assert.Equal(now.AddMinutes(5), reservation.ApprovedAt);
    }

    [Fact]
    public void Reject_FromPending_ChangesStatusAndStoresReason()
    {
        // Verify pending reservations can be rejected with a reason.
        var now = DateTimeOffset.UtcNow;
        var reservation = CreateReservation(now);

        reservation.Reject("backoffice-1", "Slot unavailable.", now.AddMinutes(5));

        Assert.Equal(ReservationStatus.Rejected, reservation.Status);
        Assert.False(reservation.IsActive);
        Assert.Equal("Slot unavailable.", reservation.RejectionReason);
    }

    [Fact]
    public void Cancel_FromApproved_ChangesStatusToCancelled()
    {
        // Verify approved reservations can be cancelled before completion.
        var now = DateTimeOffset.UtcNow;
        var reservation = CreateReservation(now);
        reservation.Approve("backoffice-1", now.AddMinutes(5));

        reservation.Cancel(now.AddMinutes(10));

        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.False(reservation.IsActive);
        Assert.Equal(now.AddMinutes(10), reservation.CancelledAt);
    }

    [Fact]
    public void Complete_FromApproved_ChangesStatusToCompleted()
    {
        // Verify approved reservations can be completed exactly once.
        var now = DateTimeOffset.UtcNow;
        var reservation = CreateReservation(now);
        reservation.Approve("operator-1", now.AddMinutes(5));

        reservation.Complete("operator-1", now.AddMinutes(30));

        Assert.Equal(ReservationStatus.Completed, reservation.Status);
        Assert.False(reservation.IsActive);
        Assert.Equal("operator-1", reservation.CompletedBy);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ThrowsInvalidOperationException()
    {
        // Verify completed reservations cannot be completed twice.
        var now = DateTimeOffset.UtcNow;
        var reservation = CreateReservation(now);
        reservation.Approve("operator-1", now.AddMinutes(5));
        reservation.Complete("operator-1", now.AddMinutes(30));

        Assert.Throws<InvalidOperationException>(() => reservation.Complete("operator-1", now.AddMinutes(40)));
    }

    [Fact]
    public void Approve_WhenCancelled_ThrowsInvalidOperationException()
    {
        // Verify cancelled reservations cannot return to approved state.
        var now = DateTimeOffset.UtcNow;
        var reservation = CreateReservation(now);
        reservation.Cancel(now.AddMinutes(5));

        Assert.Throws<InvalidOperationException>(() => reservation.Approve("backoffice-1", now.AddMinutes(10)));
    }

    [Fact]
    public void Complete_WhenRejected_ThrowsInvalidOperationException()
    {
        // Verify rejected reservations cannot be completed.
        var now = DateTimeOffset.UtcNow;
        var reservation = CreateReservation(now);
        reservation.Reject("backoffice-1", "Not available.", now.AddMinutes(5));

        Assert.Throws<InvalidOperationException>(() => reservation.Complete("operator-1", now.AddMinutes(10)));
    }

    [Fact]
    public void RegisterQrVerificationToken_WithInvalidExpiry_ThrowsArgumentException()
    {
        // Verify QR token metadata requires a future expiry.
        var now = DateTimeOffset.UtcNow;
        var reservation = CreateReservation(now);
        reservation.Approve("backoffice-1", now.AddMinutes(5));

        Assert.Throws<ArgumentException>(() => reservation.RegisterQrVerificationToken(
            "hashed-token",
            now.AddMinutes(10),
            now.AddMinutes(10)));
    }

    [Fact]
    public void MarkQrVerified_WithoutToken_ThrowsInvalidOperationException()
    {
        // Verify QR verification cannot be marked before token metadata exists.
        var now = DateTimeOffset.UtcNow;
        var reservation = CreateReservation(now);
        reservation.Approve("backoffice-1", now.AddMinutes(5));

        Assert.Throws<InvalidOperationException>(() => reservation.MarkQrVerified(now.AddMinutes(10)));
    }

    private static EnergyReservation CreateReservation(DateTimeOffset now)
    {
        // Create a valid pending reservation for transition tests.
        return EnergyReservation.Create(
            "reservation-1",
            "prosumer-1",
            "station-1",
            "slot-1",
            now.AddHours(2),
            now);
    }
}
