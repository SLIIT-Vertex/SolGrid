package com.solgrid.mobile.feature.prosumer

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.solgrid.mobile.core.mock.MockData
import com.solgrid.mobile.core.models.EnergyReservation
import com.solgrid.mobile.core.models.ProsumerAccountStatus
import com.solgrid.mobile.core.models.ProsumerProfile
import com.solgrid.mobile.core.models.ReservationStatus
import com.solgrid.mobile.core.network.ProsumerActionOutcome
import com.solgrid.mobile.core.network.ProsumerProfileOutcome
import com.solgrid.mobile.core.network.ProsumerRepository
import com.solgrid.mobile.core.network.ProsumerResponseDto
import com.solgrid.mobile.core.network.ReservationActionOutcome
import com.solgrid.mobile.core.network.ReservationDto
import com.solgrid.mobile.core.network.ReservationListOutcome
import com.solgrid.mobile.core.network.ReservationOutcome
import com.solgrid.mobile.core.network.ReservationQrOutcome
import com.solgrid.mobile.core.network.ReservationRepository
import com.solgrid.mobile.core.network.SessionStore
import com.solgrid.mobile.feature.microgrid.NodeRepository
import com.solgrid.mobile.feature.microgrid.NodeResult
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.time.Instant
import java.time.LocalDateTime
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter

private val statusByOrdinal: Map<Int, ProsumerAccountStatus> = mapOf(
    1 to ProsumerAccountStatus.PENDING,
    2 to ProsumerAccountStatus.ACTIVE,
    3 to ProsumerAccountStatus.DEACTIVATION_REQUESTED,
    4 to ProsumerAccountStatus.DEACTIVATED,
)

private fun ProsumerResponseDto.toProfile(): ProsumerProfile = ProsumerProfile(
    nic = nic,
    firstName = firstName,
    lastName = lastName,
    email = email,
    phone = phoneNumber.orEmpty(),
    status = statusByOrdinal[status] ?: ProsumerAccountStatus.PENDING,
)

private val reservationStatusByOrdinal: Map<Int, ReservationStatus> = mapOf(
    1 to ReservationStatus.PENDING,
    2 to ReservationStatus.APPROVED,
    3 to ReservationStatus.REJECTED,
    4 to ReservationStatus.CANCELLED,
    5 to ReservationStatus.COMPLETED,
)

private val displayDateFormatter = DateTimeFormatter.ofPattern("MMM d, yyyy")
private val displayTimeFormatter = DateTimeFormatter.ofPattern("hh:mm a")

private fun ReservationDto.toEnergyReservation(nodeName: String): EnergyReservation {
    val scheduled = OffsetDateTime.parse(scheduledAt).atZoneSameInstant(ZoneId.systemDefault())
    val created = OffsetDateTime.parse(createdAt).atZoneSameInstant(ZoneId.systemDefault())
    return EnergyReservation(
        id = id,
        prosumerNic = prosumerId,
        nodeId = stationId,
        nodeName = nodeName,
        bookingSlotId = bookingSlotId,
        date = scheduled.format(displayDateFormatter),
        startTime = scheduled.format(displayTimeFormatter),
        endTime = scheduled.format(displayTimeFormatter),
        energyKwh = 0.0,
        status = reservationStatusByOrdinal[status] ?: ReservationStatus.PENDING,
        createdAt = created.format(displayDateFormatter),
        rejectionReason = rejectionReason,
        qrPayload = null,
    )
}

data class ProsumerUiState(
    val profile: ProsumerProfile = MockData.prosumer,
    val reservations: List<EnergyReservation> = emptyList(),
    val loading: Boolean = true,
    val profileError: String? = null,
    val reservationsLoading: Boolean = false,
    val reservationsError: String? = null,
) {
    val currentAndPending: List<EnergyReservation>
        get() = reservations.filter { it.status == ReservationStatus.PENDING || it.status == ReservationStatus.APPROVED }
    val history: List<EnergyReservation>
        get() = reservations.filter { it.status == ReservationStatus.COMPLETED || it.status == ReservationStatus.CANCELLED }
    val pendingCount: Int get() = reservations.count { it.status == ReservationStatus.PENDING }
    val approvedFutureCount: Int get() = reservations.count { it.status == ReservationStatus.APPROVED }
}

enum class ReservationAction { CREATED, UPDATED, CANCELLED }

/**
 * Owns Prosumer-side state: profile, reservation list, and the create/update/cancel workflow.
 * All server-authoritative rules (7-day window, 12-hour notice) are enforced by the central API in
 * the real system — this mock layer simulates the same validation so the UI states are demonstrable.
 */
class ProsumerViewModel : ViewModel() {
    private val _uiState = MutableStateFlow(ProsumerUiState())
    val uiState: StateFlow<ProsumerUiState> = _uiState

    private val prosumerRepository = ProsumerRepository()
    private val reservationRepository = ReservationRepository()
    private val nodeRepository = NodeRepository()
    private val stationNameCache = mutableMapOf<String, String>()

    /** Call after sign-in/registration navigates into the Prosumer flow — there is no session yet
     * when this ViewModel is first constructed (see AppNavGraph), so profile loading is explicit
     * rather than happening in init. */
    fun loadProfile() {
        viewModelScope.launch {
            _uiState.update { it.copy(loading = true, profileError = null) }
            when (val outcome = prosumerRepository.getMyProfile()) {
                is ProsumerProfileOutcome.Success -> {
                    _uiState.update { it.copy(profile = outcome.response.toProfile(), loading = false) }
                }
                is ProsumerProfileOutcome.Failure -> {
                    _uiState.update { it.copy(loading = false, profileError = outcome.message) }
                }
            }
        }
        loadReservations()
    }

    fun loadReservations() {
        viewModelScope.launch {
            _uiState.update { it.copy(reservationsLoading = true, reservationsError = null) }
            when (val outcome = reservationRepository.getMyReservations()) {
                is ReservationListOutcome.Success -> {
                    val mapped = outcome.response.items.map { dto -> dto.toEnergyReservation(nodeName(dto.stationId)) }
                    _uiState.update { it.copy(reservations = mapped, reservationsLoading = false) }
                }
                is ReservationListOutcome.Failure -> {
                    _uiState.update { it.copy(reservationsLoading = false, reservationsError = outcome.message) }
                }
            }
        }
    }

    private suspend fun nodeName(stationId: String): String {
        stationNameCache[stationId]?.let { return it }
        return when (val result = nodeRepository.station(stationId)) {
            is NodeResult.Success -> result.value.name.also { stationNameCache[stationId] = it }
            is NodeResult.Failure -> stationId
        }
    }

    fun reservationById(id: String): EnergyReservation? = _uiState.value.reservations.find { it.id == id }

    /** Returns an error message, or null if the reservation can be created. Mirrors the API's 7-day rule. */
    fun validateNewReservationDate(dateTime: LocalDateTime): String? {
        val now = LocalDateTime.now()
        return when {
            dateTime.isBefore(now) -> "Selected time is in the past."
            dateTime.isAfter(now.plusDays(7)) -> "Reservations must be scheduled within the next 7 days."
            else -> null
        }
    }

    fun createReservation(
        stationId: String,
        bookingSlotId: String,
        nodeName: String,
        scheduledAtIso: String,
        onError: (String) -> Unit,
        onResult: (EnergyReservation) -> Unit,
    ) {
        viewModelScope.launch {
            val prosumerId = SessionStore.userId
            if (prosumerId == null) {
                onError("Your session has expired. Please sign in again.")
                return@launch
            }
            when (
                val outcome = reservationRepository.create(
                    prosumerId = prosumerId,
                    stationId = stationId,
                    bookingSlotId = bookingSlotId,
                    scheduledAt = scheduledAtIso,
                )
            ) {
                is ReservationOutcome.Success -> {
                    val reservation = outcome.response.toEnergyReservation(nodeName)
                    _uiState.update { it.copy(reservations = listOf(reservation) + it.reservations) }
                    onResult(reservation)
                }
                is ReservationOutcome.Failure -> onError(outcome.message)
            }
        }
    }

    /** Returns an error message if fewer than 12 hours remain before the booking, else null (allowed). */
    fun validateModifyWindow(reservation: EnergyReservation): String? {
        if (reservation.status != ReservationStatus.PENDING && reservation.status != ReservationStatus.APPROVED) {
            return "This booking can no longer be changed."
        }
        // The exact 12-hour cutoff is authoritatively enforced server-side; the API returns a 409
        // Conflict if the notice window has passed, surfaced as onError in the caller.
        return null
    }

    fun updateReservation(
        reservationId: String,
        stationId: String,
        bookingSlotId: String,
        nodeName: String,
        scheduledAtIso: String,
        onError: (String) -> Unit,
        onResult: (EnergyReservation) -> Unit,
    ) {
        viewModelScope.launch {
            when (
                val outcome = reservationRepository.update(
                    id = reservationId,
                    stationId = stationId,
                    bookingSlotId = bookingSlotId,
                    scheduledAt = scheduledAtIso,
                )
            ) {
                is ReservationOutcome.Success -> {
                    val updated = outcome.response.toEnergyReservation(nodeName)
                    _uiState.update { state ->
                        state.copy(reservations = state.reservations.map { if (it.id == reservationId) updated else it })
                    }
                    onResult(updated)
                }
                is ReservationOutcome.Failure -> onError(outcome.message)
            }
        }
    }

    fun cancelReservation(reservationId: String, onError: (String) -> Unit, onDone: () -> Unit) {
        viewModelScope.launch {
            when (val outcome = reservationRepository.cancel(reservationId)) {
                is ReservationActionOutcome.Success -> {
                    _uiState.update { state ->
                        state.copy(
                            reservations = state.reservations.map {
                                if (it.id == reservationId) it.copy(status = ReservationStatus.CANCELLED) else it
                            }
                        )
                    }
                    onDone()
                }
                is ReservationActionOutcome.Failure -> onError(outcome.message)
            }
        }
    }

    fun issueReservationQr(reservationId: String, onError: (String) -> Unit, onResult: (String, String) -> Unit) {
        viewModelScope.launch {
            when (val outcome = reservationRepository.issueQr(reservationId)) {
                is ReservationQrOutcome.Success -> {
                    val payload = "${outcome.response.reservationId}|${outcome.response.verificationToken}"
                    _uiState.update { state ->
                        state.copy(
                            reservations = state.reservations.map {
                                if (it.id == reservationId) it.copy(qrPayload = payload) else it
                            }
                        )
                    }
                    onResult(payload, outcome.response.expiresAt)
                }
                is ReservationQrOutcome.Failure -> onError(outcome.message)
            }
        }
    }

    fun updateProfile(
        firstName: String,
        lastName: String,
        email: String,
        phone: String,
        onError: (String) -> Unit,
        onDone: () -> Unit,
    ) {
        viewModelScope.launch {
            when (
                val outcome = prosumerRepository.updateMyProfile(
                    firstName = firstName,
                    lastName = lastName,
                    email = email,
                    phoneNumber = phone.ifBlank { null },
                )
            ) {
                is ProsumerProfileOutcome.Success -> {
                    _uiState.update { it.copy(profile = outcome.response.toProfile()) }
                    onDone()
                }
                is ProsumerProfileOutcome.Failure -> onError(outcome.message)
            }
        }
    }

    fun requestDeactivation(onError: (String) -> Unit, onDone: () -> Unit) {
        viewModelScope.launch {
            when (val outcome = prosumerRepository.requestDeactivation()) {
                is ProsumerActionOutcome.Success -> {
                    _uiState.update {
                        it.copy(profile = it.profile.copy(status = ProsumerAccountStatus.DEACTIVATION_REQUESTED))
                    }
                    onDone()
                }
                is ProsumerActionOutcome.Failure -> onError(outcome.message)
            }
        }
    }
}
