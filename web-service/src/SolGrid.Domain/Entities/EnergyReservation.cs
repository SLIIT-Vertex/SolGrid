/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: EnergyReservation.cs
 * Description: Represents an energy slot reservation and its lifecycle state.
 * Contributor: Dilshan Yapa S Y C T
 */

using SolGrid.Domain.Enums;

namespace SolGrid.Domain.Entities;

public sealed class EnergyReservation
{
    private EnergyReservation(
        string id,
        string referenceCode,
        string prosumerId,
        string stationId,
        string bookingSlotId,
        DateTimeOffset scheduledAt,
        ReservationStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        DateTimeOffset? approvedAt,
        string? approvedBy,
        DateTimeOffset? rejectedAt,
        string? rejectedBy,
        string? rejectionReason,
        DateTimeOffset? cancelledAt,
        DateTimeOffset? completedAt,
        string? completedBy,
        string? qrVerificationTokenHash,
        DateTimeOffset? qrVerificationTokenIssuedAt,
        DateTimeOffset? qrVerificationTokenExpiresAt,
        DateTimeOffset? qrVerifiedAt,
        int version)
    {
        // Initialize a reservation from validated creation or persistence values.
        Id = RequireValue(id, nameof(id));
        ReferenceCode = RequireValue(referenceCode, nameof(referenceCode));
        ProsumerId = RequireValue(prosumerId, nameof(prosumerId));
        StationId = RequireValue(stationId, nameof(stationId));
        BookingSlotId = RequireValue(bookingSlotId, nameof(bookingSlotId));
        ScheduledAt = scheduledAt;
        Status = RequireDefinedEnum(status, nameof(status));
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        ApprovedAt = approvedAt;
        ApprovedBy = TrimOptional(approvedBy);
        RejectedAt = rejectedAt;
        RejectedBy = TrimOptional(rejectedBy);
        RejectionReason = TrimOptional(rejectionReason);
        CancelledAt = cancelledAt;
        CompletedAt = completedAt;
        CompletedBy = TrimOptional(completedBy);
        QrVerificationTokenHash = TrimOptional(qrVerificationTokenHash);
        QrVerificationTokenIssuedAt = qrVerificationTokenIssuedAt;
        QrVerificationTokenExpiresAt = qrVerificationTokenExpiresAt;
        QrVerifiedAt = qrVerifiedAt;
        Version = RequireValidVersion(version);
        ValidateLifecycleState();
    }

    public string Id { get; }

    /** Short, human-friendly booking reference (e.g. "SG-3F9A2C10") shown to prosumers and operators
     * instead of the raw 32-char id. Generated once at creation and stable for the reservation's life. */
    public string ReferenceCode { get; }

    public string ProsumerId { get; }

    public string StationId { get; private set; }

    public string BookingSlotId { get; private set; }

    public DateTimeOffset ScheduledAt { get; private set; }

    public ReservationStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? ApprovedAt { get; private set; }

    public string? ApprovedBy { get; private set; }

    public DateTimeOffset? RejectedAt { get; private set; }

    public string? RejectedBy { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string? CompletedBy { get; private set; }

    public string? QrVerificationTokenHash { get; private set; }

    public DateTimeOffset? QrVerificationTokenIssuedAt { get; private set; }

    public DateTimeOffset? QrVerificationTokenExpiresAt { get; private set; }

    public DateTimeOffset? QrVerifiedAt { get; private set; }

    public int Version { get; private set; }

    public bool IsActive => Status is ReservationStatus.Pending or ReservationStatus.Approved;

    public static EnergyReservation Create(
        string id,
        string prosumerId,
        string stationId,
        string bookingSlotId,
        DateTimeOffset scheduledAt,
        DateTimeOffset createdAt)
    {
        // Create a new pending reservation with required references and a fresh booking reference.
        return new EnergyReservation(
            id,
            GenerateReferenceCode(),
            prosumerId,
            stationId,
            bookingSlotId,
            scheduledAt,
            ReservationStatus.Pending,
            createdAt,
            createdAt,
            approvedAt: null,
            approvedBy: null,
            rejectedAt: null,
            rejectedBy: null,
            rejectionReason: null,
            cancelledAt: null,
            completedAt: null,
            completedBy: null,
            qrVerificationTokenHash: null,
            qrVerificationTokenIssuedAt: null,
            qrVerificationTokenExpiresAt: null,
            qrVerifiedAt: null,
            version: 0);
    }

    public static EnergyReservation Restore(
        string id,
        string prosumerId,
        string stationId,
        string bookingSlotId,
        DateTimeOffset scheduledAt,
        ReservationStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        DateTimeOffset? approvedAt,
        string? approvedBy,
        DateTimeOffset? rejectedAt,
        string? rejectedBy,
        string? rejectionReason,
        DateTimeOffset? cancelledAt,
        DateTimeOffset? completedAt,
        string? completedBy,
        string? qrVerificationTokenHash,
        DateTimeOffset? qrVerificationTokenIssuedAt,
        DateTimeOffset? qrVerificationTokenExpiresAt,
        DateTimeOffset? qrVerifiedAt,
        int version = 0,
        string? referenceCode = null)
    {
        // Rehydrate a reservation without leaking persistence-specific types into Domain.
        // Legacy rows persisted before booking references derive a stable code from the id.
        return new EnergyReservation(
            id,
            string.IsNullOrWhiteSpace(referenceCode) ? DeriveReferenceCode(id) : referenceCode,
            prosumerId,
            stationId,
            bookingSlotId,
            scheduledAt,
            status,
            createdAt,
            updatedAt,
            approvedAt,
            approvedBy,
            rejectedAt,
            rejectedBy,
            rejectionReason,
            cancelledAt,
            completedAt,
            completedBy,
            qrVerificationTokenHash,
            qrVerificationTokenIssuedAt,
            qrVerificationTokenExpiresAt,
            qrVerifiedAt,
            version);
    }

    public void Reschedule(DateTimeOffset scheduledAt, DateTimeOffset updatedAt)
    {
        // Change the reservation schedule while it is still active.
        EnsureStatus([ReservationStatus.Pending, ReservationStatus.Approved], "Only pending or approved reservations can be rescheduled.");
        ScheduledAt = scheduledAt;
        MarkUpdated(updatedAt);
    }

    public void Reschedule(
        string stationId,
        string bookingSlotId,
        DateTimeOffset scheduledAt,
        DateTimeOffset updatedAt)
    {
        // Change the reservation station, slot, and schedule while it is still active.
        EnsureStatus([ReservationStatus.Pending, ReservationStatus.Approved], "Only pending or approved reservations can be rescheduled.");
        StationId = RequireValue(stationId, nameof(stationId));
        BookingSlotId = RequireValue(bookingSlotId, nameof(bookingSlotId));
        ScheduledAt = scheduledAt;
        MarkUpdated(updatedAt);
    }

    public void Approve(string approvedBy, DateTimeOffset approvedAt)
    {
        // Approve a pending reservation and capture approver metadata.
        EnsureStatus([ReservationStatus.Pending], "Only pending reservations can be approved.");
        Status = ReservationStatus.Approved;
        ApprovedBy = RequireValue(approvedBy, nameof(approvedBy));
        ApprovedAt = approvedAt;
        MarkUpdated(approvedAt);
    }

    public void Reject(string rejectedBy, string rejectionReason, DateTimeOffset rejectedAt)
    {
        // Reject a pending reservation and capture rejection metadata.
        EnsureStatus([ReservationStatus.Pending], "Only pending reservations can be rejected.");
        Status = ReservationStatus.Rejected;
        RejectedBy = RequireValue(rejectedBy, nameof(rejectedBy));
        RejectionReason = RequireValue(rejectionReason, nameof(rejectionReason));
        RejectedAt = rejectedAt;
        MarkUpdated(rejectedAt);
    }

    public void Cancel(DateTimeOffset cancelledAt)
    {
        // Cancel a pending or approved reservation.
        EnsureStatus([ReservationStatus.Pending, ReservationStatus.Approved], "Only pending or approved reservations can be cancelled.");
        Status = ReservationStatus.Cancelled;
        CancelledAt = cancelledAt;
        MarkUpdated(cancelledAt);
    }

    public void RegisterQrVerificationToken(
        string tokenHash,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
    {
        // Attach QR verification token metadata to an approved reservation.
        EnsureStatus([ReservationStatus.Approved], "Only approved reservations can receive QR verification tokens.");
        if (expiresAt <= issuedAt)
        {
            throw new ArgumentException("QR token expiry must be after issue time.", nameof(expiresAt));
        }

        QrVerificationTokenHash = RequireValue(tokenHash, nameof(tokenHash));
        QrVerificationTokenIssuedAt = issuedAt;
        QrVerificationTokenExpiresAt = expiresAt;
        MarkUpdated(issuedAt);
    }

    public void MarkQrVerified(DateTimeOffset verifiedAt)
    {
        // Record successful QR verification for an approved reservation.
        EnsureStatus([ReservationStatus.Approved], "Only approved reservations can be QR verified.");
        if (string.IsNullOrWhiteSpace(QrVerificationTokenHash))
        {
            throw new InvalidOperationException("QR verification token is not registered.");
        }

        QrVerifiedAt = verifiedAt;
        MarkUpdated(verifiedAt);
    }

    public void Complete(string completedBy, DateTimeOffset completedAt)
    {
        // Complete an approved reservation exactly once.
        EnsureStatus([ReservationStatus.Approved], "Only approved reservations can be completed.");
        Status = ReservationStatus.Completed;
        CompletedBy = RequireValue(completedBy, nameof(completedBy));
        CompletedAt = completedAt;
        MarkUpdated(completedAt);
    }

    private void EnsureStatus(IReadOnlyCollection<ReservationStatus> allowedStatuses, string message)
    {
        // Reject invalid status transitions from the current reservation state.
        if (!allowedStatuses.Contains(Status))
        {
            throw new InvalidOperationException(message);
        }
    }

    private void ValidateLifecycleState()
    {
        // Ensure restored reservation metadata matches its lifecycle state.
        if (Status == ReservationStatus.Approved && (ApprovedAt is null || string.IsNullOrWhiteSpace(ApprovedBy)))
        {
            throw new ArgumentException("Approved reservations require approval metadata.");
        }

        if (Status == ReservationStatus.Rejected && (RejectedAt is null || string.IsNullOrWhiteSpace(RejectedBy)))
        {
            throw new ArgumentException("Rejected reservations require rejection metadata.");
        }

        if (Status == ReservationStatus.Cancelled && CancelledAt is null)
        {
            throw new ArgumentException("Cancelled reservations require cancellation metadata.");
        }

        if (Status == ReservationStatus.Completed && (CompletedAt is null || string.IsNullOrWhiteSpace(CompletedBy)))
        {
            throw new ArgumentException("Completed reservations require completion metadata.");
        }
    }

    private static string RequireValue(string value, string parameterName)
    {
        // Reject missing text values before they enter the reservation model.
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    private static string GenerateReferenceCode()
    {
        // Produce a compact, readable booking reference (e.g. "SG-3F9A2C10") for display to users.
        return "SG-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    }

    private static string DeriveReferenceCode(string id)
    {
        // Fallback used when rehydrating legacy reservations stored before references existed.
        var cleaned = new string((id ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
        return cleaned.Length == 0
            ? GenerateReferenceCode()
            : "SG-" + cleaned[^Math.Min(8, cleaned.Length)..].ToUpperInvariant();
    }

    private static string? TrimOptional(string? value)
    {
        // Normalize optional text values without turning missing values into content.
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static TEnum RequireDefinedEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        // Reject undefined enum values before they become domain state.
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value is not supported.");
        }

        return value;
    }

    private static int RequireValidVersion(int version)
    {
        // Reject invalid persistence concurrency versions before restoring domain state.
        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version), version, "Version cannot be negative.");
        }

        return version;
    }

    private void MarkUpdated(DateTimeOffset updatedAt)
    {
        // Capture the latest reservation state change timestamp and advance its concurrency version.
        UpdatedAt = updatedAt;
        Version++;
    }
}
