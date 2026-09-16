package com.solgrid.mobile.core.network

import kotlinx.serialization.Serializable

/**
 * Wire-format DTOs mirroring the backend's SolGrid.Application.Auth request/response contracts
 * (POST /api/v1/auth/login). ASP.NET Core's default System.Text.Json settings serialize to
 * camelCase, so field names here follow that casing rather than the C# PascalCase property names.
 */
@Serializable
data class LoginRequestDto(
    val email: String,
    val password: String
)

@Serializable
data class LoginResponseDto(
    val accessToken: String,
    val expiresAt: String,
    val user: UserResponseDto
)

/**
 * `role`/`status` arrive as raw enum ordinals (ASP.NET's default System.Text.Json serialization
 * for enums), not strings — see SolGrid.Domain.Enums.UserRole (Backoffice = 1, GridOperator = 2)
 * and AccountStatus.
 */
@Serializable
data class UserResponseDto(
    val id: String,
    val firstName: String,
    val lastName: String,
    val email: String,
    val role: Int,
    val status: Int,
    val createdAt: String,
    val updatedAt: String
)

@Serializable
data class ProblemDetailsDto(
    val title: String? = null,
    val detail: String? = null,
    val status: Int? = null
)

@Serializable
data class PagedResponseDto<T>(val items: List<T>, val totalCount: Long, val pageNumber: Int, val pageSize: Int)

@Serializable
data class GeoCoordinatesDto(val latitude: Double, val longitude: Double)

@Serializable
data class OperatingWindowDto(val day: Int, val opensAt: String, val closesAt: String)

@Serializable
data class SolarStationDto(
    val id: String, val code: String, val name: String, val addressLine: String,
    val location: GeoCoordinatesDto, val capacityKw: Double, val status: Int,
    val totalSlotCount: Int, val availableSlotCount: Int,
    val schedule: List<OperatingWindowDto> = emptyList(), val distanceKilometers: Double? = null,
)

@Serializable
data class BookingSlotDto(
    val id: String, val stationId: String, val startTime: String, val endTime: String,
    val slotNumber: Int, val batteryCapacityKwh: Double, val status: Int,
    val isActive: Boolean, val isAvailable: Boolean,
)

/** `status` is a raw ordinal — see SolGrid.Domain.Enums.ReservationStatus (Pending=1, Approved=2,
 * Rejected=3, Cancelled=4, Completed=5). */
@Serializable
data class ReservationDto(
    val id: String, val prosumerId: String, val stationId: String, val bookingSlotId: String,
    val scheduledAt: String, val status: Int, val createdAt: String, val updatedAt: String = createdAt,
    val approvedAt: String? = null, val approvedBy: String? = null,
    val rejectedAt: String? = null, val rejectedBy: String? = null, val rejectionReason: String? = null,
    val cancelledAt: String? = null,
    val completedAt: String? = null, val completedBy: String? = null,
    val hasQrVerificationToken: Boolean = false,
    val qrVerificationTokenIssuedAt: String? = null, val qrVerificationTokenExpiresAt: String? = null,
    val qrVerifiedAt: String? = null,
)

@Serializable
data class CreateReservationRequestDto(
    val prosumerId: String, val stationId: String, val bookingSlotId: String, val scheduledAt: String,
)

@Serializable
data class UpdateReservationRequestDto(
    val stationId: String, val bookingSlotId: String, val scheduledAt: String,
)

@Serializable
data class RejectReservationRequestDto(val rejectionReason: String)

@Serializable
data class ReservationQrResponseDto(
    val reservationId: String, val verificationToken: String, val expiresAt: String,
)

@Serializable
data class VerifyReservationQrRequestDto(val reservationId: String, val verificationToken: String)

@Serializable
data class VerifyReservationQrResponseDto(val isValid: Boolean, val reservationId: String, val message: String)

@Serializable
data class CompleteReservationRequestDto(val verificationToken: String)

/** POST /api/v1/prosumers/register request body. */
@Serializable
data class RegisterProsumerRequestDto(
    val nic: String,
    val firstName: String,
    val lastName: String,
    val email: String,
    val phoneNumber: String? = null,
    val password: String
)

/** PUT /api/v1/prosumers/me request body — NIC is immutable, not included. */
@Serializable
data class UpdateProsumerRequestDto(
    val firstName: String,
    val lastName: String,
    val email: String,
    val phoneNumber: String? = null
)

@Serializable
data class ProsumerLoginResponseDto(
    val accessToken: String,
    val expiresAt: String,
    val prosumer: ProsumerResponseDto
)

/** `status` arrives as a raw ordinal — see SolGrid.Domain.Enums.ProsumerAccountStatus (Pending=1,
 * Active=2, DeactivationRequested=3, Deactivated=4). */
@Serializable
data class ProsumerResponseDto(
    val nic: String,
    val firstName: String,
    val lastName: String,
    val email: String,
    val phoneNumber: String? = null,
    val status: Int,
    val createdAt: String,
    val updatedAt: String
)

@Serializable
data class ReservationDashboardSummaryDto(val pendingReservationsCount: Long, val approvedFutureReservationsCount: Long, val currentReservationsCount: Long, val bookingHistoryCount: Long)
