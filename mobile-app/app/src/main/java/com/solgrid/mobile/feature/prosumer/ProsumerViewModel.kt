package com.solgrid.mobile.feature.prosumer

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.solgrid.mobile.core.mock.MockData
import com.solgrid.mobile.core.models.EnergyReservation
import com.solgrid.mobile.core.models.ProsumerAccountStatus
import com.solgrid.mobile.core.models.ProsumerProfile
import com.solgrid.mobile.core.models.ReservationStatus
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter

data class ProsumerUiState(
    val profile: ProsumerProfile = MockData.prosumer,
    val reservations: List<EnergyReservation> = MockData.reservations.toList(),
    val loading: Boolean = true
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

    private var nextId = 2000

    init {
        viewModelScope.launch {
            delay(600)
            _uiState.update { it.copy(loading = false) }
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
        nodeId: String,
        nodeName: String,
        date: String,
        startTime: String,
        endTime: String,
        energyKwh: Double,
        onResult: (EnergyReservation) -> Unit
    ) {
        viewModelScope.launch {
            delay(700)
            val reservation = EnergyReservation(
                id = "res-${nextId++}",
                prosumerNic = _uiState.value.profile.nic,
                nodeId = nodeId,
                nodeName = nodeName,
                date = date,
                startTime = startTime,
                endTime = endTime,
                energyKwh = energyKwh,
                status = ReservationStatus.PENDING,
                createdAt = "Just now"
            )
            _uiState.update { it.copy(reservations = listOf(reservation) + it.reservations) }
            onResult(reservation)
        }
    }

    /** Returns an error message if fewer than 12 hours remain before the booking, else null (allowed). */
    fun validateModifyWindow(reservation: EnergyReservation): String? {
        // Mock check: PENDING bookings are treated as always modifiable; APPROVED ones simulate the
        // 12-hour cutoff based on a fixed "now" for demo purposes.
        return if (reservation.status == ReservationStatus.APPROVED && reservation.id == "res-1001") {
            null // demo booking is > 12h away — allowed
        } else if (reservation.status == ReservationStatus.PENDING) {
            null
        } else {
            "This booking is less than 12 hours away and can no longer be changed."
        }
    }

    fun updateReservation(
        reservationId: String,
        date: String,
        startTime: String,
        endTime: String,
        onResult: (EnergyReservation) -> Unit
    ) {
        viewModelScope.launch {
            delay(700)
            var updated: EnergyReservation? = null
            _uiState.update { state ->
                state.copy(
                    reservations = state.reservations.map {
                        if (it.id == reservationId) {
                            it.copy(date = date, startTime = startTime, endTime = endTime).also { r -> updated = r }
                        } else it
                    }
                )
            }
            updated?.let(onResult)
        }
    }

    fun cancelReservation(reservationId: String, onDone: () -> Unit) {
        viewModelScope.launch {
            delay(600)
            _uiState.update { state ->
                state.copy(
                    reservations = state.reservations.map {
                        if (it.id == reservationId) it.copy(status = ReservationStatus.CANCELLED) else it
                    }
                )
            }
            onDone()
        }
    }

    fun updateProfile(fullName: String, email: String, phone: String, address: String) {
        _uiState.update { it.copy(profile = it.profile.copy(fullName = fullName, email = email, phone = phone, address = address)) }
    }

    fun requestDeactivation(onDone: () -> Unit) {
        viewModelScope.launch {
            delay(700)
            _uiState.update { it.copy(profile = it.profile.copy(status = ProsumerAccountStatus.DEACTIVATED)) }
            onDone()
        }
    }
}
