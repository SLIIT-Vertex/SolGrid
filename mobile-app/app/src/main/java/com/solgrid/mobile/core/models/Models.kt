package com.solgrid.mobile.core.models

/**
 * Domain models for the Smart Solar Microgrid Trading System mobile client.
 *
 * These mirror the shapes the central C# Web API / MongoDB collections are expected to expose
 * (see: User's detail, SolarStationInfo, EnergyBookingSlots, Energy Reservation). The mobile app
 * is a UI client only — all authoritative validation (7-day window, 12-hour rule, reactivation,
 * node-deactivation checks) happens server-side; these models and the mock repositories only
 * simulate those responses for frontend demonstration.
 */

/** The two mobile-facing roles. Backoffice is web-only per the assignment brief. */
enum class AppRole { PROSUMER, GRID_OPERATOR }

enum class ProsumerAccountStatus { PENDING, ACTIVE, DEACTIVATION_REQUESTED, DEACTIVATED }

/**
 * A registered Solar Prosumer. NIC is the primary/unique identity, per the assignment rule.
 * Mirrors SolGrid.Application.Prosumers.Responses.ProsumerResponse — the backend has no address
 * field, so none is modeled here.
 */
data class ProsumerProfile(
    val nic: String,
    val firstName: String,
    val lastName: String,
    val email: String,
    val phone: String,
    val status: ProsumerAccountStatus = ProsumerAccountStatus.PENDING
) {
    val fullName: String get() = "$firstName $lastName"
}

/** A Grid Operator's mobile profile (operational role, not Backoffice). */
data class OperatorProfile(
    val operatorId: String,
    val fullName: String,
    val email: String,
    val assignedNodeName: String
)

enum class NodeStatus { ACTIVE, INACTIVE }

/** Mirrors the SolarStationInfo collection: a microgrid hub/node. */
data class MicrogridNode(
    val id: String,
    val name: String,
    val address: String,
    val latitude: Double,
    val longitude: Double,
    val capacityKw: Double,
    val totalSlots: Int,
    val availableSlots: Int,
    val status: NodeStatus,
    val distanceKm: Double? = null
)

enum class SlotStatus { AVAILABLE, RESERVED, UNAVAILABLE }

/** Mirrors an entry in EnergyBookingSlots for a given node. */
data class BookingSlot(
    val id: String,
    val nodeId: String,
    val date: String,
    val startTime: String,
    val endTime: String,
    val status: SlotStatus
)

enum class ReservationStatus { PENDING, APPROVED, REJECTED, CANCELLED, COMPLETED }

/** Mirrors an Energy Reservation document (SolGrid.Application.Reservations.Responses.ReservationResponse). */
data class EnergyReservation(
    val id: String,
    val prosumerNic: String,
    val nodeId: String,
    val nodeName: String,
    val bookingSlotId: String = "",
    val date: String,
    val startTime: String,
    val endTime: String,
    val energyKwh: Double,
    val status: ReservationStatus,
    val createdAt: String,
    val rejectionReason: String? = null,
    /** Populated once the reservation is approved; drives the transaction QR screen. */
    val qrPayload: String? = null,
    val scheduledAt: String? = null
) {
    /** Whether this booking is still within the 12-hour modify/cancel window (mock check). */
    val canModify: Boolean
        get() = status == ReservationStatus.PENDING || status == ReservationStatus.APPROVED
}

enum class TransferVerificationResult { VALID, EXPIRED, ALREADY_COMPLETED, NOT_FOUND }

/** Result of a Grid Operator scanning a prosumer's transaction QR. */
data class QrVerification(
    val reservation: EnergyReservation?,
    val result: TransferVerificationResult
)
