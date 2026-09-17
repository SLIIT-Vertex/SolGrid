package com.solgrid.mobile.feature.operator

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.solgrid.mobile.core.models.EnergyReservation
import com.solgrid.mobile.core.models.OperatorProfile
import com.solgrid.mobile.core.models.QrVerification
import com.solgrid.mobile.core.models.ReservationStatus
import com.solgrid.mobile.core.models.TransferVerificationResult
import com.solgrid.mobile.core.network.ReservationDto
import com.solgrid.mobile.core.network.ReservationListOutcome
import com.solgrid.mobile.core.network.ReservationOutcome
import com.solgrid.mobile.core.network.ReservationRepository
import com.solgrid.mobile.core.network.VerifyQrOutcome
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter

private val displayDateFormatter = DateTimeFormatter.ofPattern("MMM d, yyyy")
private val displayTimeFormatter = DateTimeFormatter.ofPattern("hh:mm a")

private val reservationStatusByOrdinal: Map<Int, ReservationStatus> = mapOf(
    1 to ReservationStatus.PENDING,
    2 to ReservationStatus.APPROVED,
    3 to ReservationStatus.REJECTED,
    4 to ReservationStatus.CANCELLED,
    5 to ReservationStatus.COMPLETED,
)

private fun ReservationDto.toEnergyReservation(): EnergyReservation {
    val scheduled = OffsetDateTime.parse(scheduledAt).atZoneSameInstant(ZoneId.systemDefault())
    val created = OffsetDateTime.parse(createdAt).atZoneSameInstant(ZoneId.systemDefault())
    return EnergyReservation(
        id = id,
        prosumerNic = prosumerId,
        nodeId = stationId,
        nodeName = stationId,
        bookingSlotId = bookingSlotId,
        date = scheduled.format(displayDateFormatter),
        startTime = scheduled.format(displayTimeFormatter),
        endTime = scheduled.format(displayTimeFormatter),
        energyKwh = 0.0,
        status = reservationStatusByOrdinal[status] ?: ReservationStatus.PENDING,
        createdAt = created.format(displayDateFormatter),
        rejectionReason = rejectionReason,
    )
}

data class OperatorUiState(
    val profile: OperatorProfile = OperatorProfile(com.solgrid.mobile.core.network.SessionStore.userId.orEmpty(), com.solgrid.mobile.core.network.SessionStore.userDisplayName.orEmpty(), "", "All grid nodes"),
    val queue: List<EnergyReservation> = emptyList(),
    val queueError: String? = null,
    val queueLoading: Boolean = false,
    val lastVerification: QrVerification? = null,
    val lastVerificationToken: String? = null,
    val verifying: Boolean = false,
    val operationError: String? = null,
    val finalizing: Boolean = false
)

/**
 * Grid Operator mobile flow: scan a prosumer's transaction QR, verify it against the server, then
 * finalize the energy transfer via POST /api/v1/reservations/verify-qr and .../complete. The scanned
 * payload is "{reservationId}|{verificationToken}", matching what ReservationQrScreen encodes.
 */
class OperatorViewModel(private val reservationRepository: ReservationRepository = ReservationRepository()) : ViewModel() {
    private val _uiState = MutableStateFlow(OperatorUiState())
    val uiState: StateFlow<OperatorUiState> = _uiState

    /** Loads the global pending/approved queue (GET /reservations/current) — the backend has no
     * per-operator station assignment, so every Backoffice/GridOperator caller sees the same queue. */
    fun loadQueue() {
        viewModelScope.launch {
            _uiState.update { it.copy(queueLoading = true, queueError = null, profile = it.profile.copy(operatorId = com.solgrid.mobile.core.network.SessionStore.userId.orEmpty(), fullName = com.solgrid.mobile.core.network.SessionStore.userDisplayName.orEmpty())) }
            when (val outcome = reservationRepository.getAllCurrent()) {
                is ReservationListOutcome.Success -> {
                    _uiState.update { it.copy(queue = outcome.response.items.map { dto -> dto.toEnergyReservation() }, queueLoading = false) }
                }
                is ReservationListOutcome.Failure -> _uiState.update { it.copy(queue = emptyList(), queueError = outcome.message, queueLoading = false) }
            }
        }
    }

    fun verifyCode(code: String) {
        val payload = com.solgrid.mobile.core.qr.TransactionQr.parse(code)
        if (payload == null) {
            _uiState.update {
                it.copy(lastVerification = QrVerification(null, TransferVerificationResult.NOT_FOUND), lastVerificationToken = null, operationError = "Invalid transaction QR payload.")
            }
            return
        }
        val (reservationId, token) = payload

        viewModelScope.launch {
            _uiState.update { it.copy(verifying = true, lastVerification = null, lastVerificationToken = null, operationError = null) }
            when (val outcome = reservationRepository.verifyQr(reservationId, token)) {
                is VerifyQrOutcome.Success -> {
                    val result = if (outcome.response.isValid) TransferVerificationResult.VALID else TransferVerificationResult.EXPIRED
                    // The verify-qr response confirms validity but does not include the full
                    // reservation; fetch it so the confirmation screen can show booking details.
                    val reservation = fetchReservation(reservationId)
                    _uiState.update {
                        it.copy(
                            verifying = false,
                            lastVerification = QrVerification(reservation, if (reservation == null) TransferVerificationResult.NOT_FOUND else result),
                            lastVerificationToken = if (reservation != null && outcome.response.isValid) token else null,
                            operationError = if (reservation == null) "Could not load the booking details. Scan again." else if (!outcome.response.isValid) outcome.response.message else null,
                        )
                    }
                }
                is VerifyQrOutcome.Failure -> {
                    _uiState.update {
                        it.copy(
                            verifying = false,
                            lastVerification = QrVerification(null, TransferVerificationResult.NOT_FOUND),
                            lastVerificationToken = null,
                            operationError = outcome.message,
                        )
                    }
                }
            }
        }
    }

    private suspend fun fetchReservation(reservationId: String): EnergyReservation? {
        return when (val outcome = reservationRepository.getById(reservationId)) {
            is ReservationOutcome.Success -> outcome.response.toEnergyReservation()
            is ReservationOutcome.Failure -> null
        }
    }

    fun finalizeTransfer(onDone: () -> Unit) {
        val reservation = _uiState.value.lastVerification?.reservation ?: return
        val token = _uiState.value.lastVerificationToken ?: return
        viewModelScope.launch {
            _uiState.update { it.copy(finalizing = true, operationError = null) }
            when (val outcome = reservationRepository.complete(reservation.id, token)) {
                is ReservationOutcome.Success -> {
                    val updated = outcome.response.toEnergyReservation()
                    _uiState.update {
                        it.copy(
                            finalizing = false,
                            queue = it.queue.map { r -> if (r.id == updated.id) updated else r },
                            lastVerification = it.lastVerification?.copy(reservation = updated),
                        )
                    }
                    onDone()
                }
                is ReservationOutcome.Failure -> {
                    _uiState.update { it.copy(finalizing = false, operationError = outcome.message) }
                }
            }
        }
    }

    fun clearVerification() = _uiState.update { it.copy(lastVerification = null, lastVerificationToken = null, operationError = null) }
}
