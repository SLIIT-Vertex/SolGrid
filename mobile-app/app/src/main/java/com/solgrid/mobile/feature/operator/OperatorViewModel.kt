package com.solgrid.mobile.feature.operator

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.solgrid.mobile.core.mock.MockData
import com.solgrid.mobile.core.models.EnergyReservation
import com.solgrid.mobile.core.models.OperatorProfile
import com.solgrid.mobile.core.models.QrVerification
import com.solgrid.mobile.core.models.ReservationStatus
import com.solgrid.mobile.core.models.TransferVerificationResult
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class OperatorUiState(
    val profile: OperatorProfile = MockData.operator,
    val queue: List<EnergyReservation> = MockData.operatorQueue,
    val lastVerification: QrVerification? = null,
    val verifying: Boolean = false,
    val finalizing: Boolean = false
)

/**
 * Grid Operator mobile flow: scan a prosumer's transaction QR, verify it against the (mock)
 * server state, then finalize the energy transfer. `POST /api/operator/verify-qr` and
 * `POST /api/operator/finalize-transfer` are the real endpoints this simulates.
 */
class OperatorViewModel : ViewModel() {
    private val _uiState = MutableStateFlow(OperatorUiState())
    val uiState: StateFlow<OperatorUiState> = _uiState

    /** Simulates scanning a code. Pass any string — "valid" maps to the demo approved booking. */
    fun verifyCode(code: String) {
        viewModelScope.launch {
            _uiState.update { it.copy(verifying = true) }
            delay(900)
            val reservation = MockData.reservations.find { it.qrPayload == code || code.contains(it.id) }
            val result = when {
                reservation == null -> TransferVerificationResult.NOT_FOUND
                reservation.status == ReservationStatus.COMPLETED -> TransferVerificationResult.ALREADY_COMPLETED
                reservation.status != ReservationStatus.APPROVED -> TransferVerificationResult.EXPIRED
                else -> TransferVerificationResult.VALID
            }
            _uiState.update {
                it.copy(verifying = false, lastVerification = QrVerification(reservation, result))
            }
        }
    }

    fun finalizeTransfer(onDone: () -> Unit) {
        val reservation = _uiState.value.lastVerification?.reservation ?: return
        viewModelScope.launch {
            _uiState.update { it.copy(finalizing = true) }
            delay(800)
            _uiState.update {
                it.copy(
                    finalizing = false,
                    queue = it.queue.map { r -> if (r.id == reservation.id) r.copy(status = ReservationStatus.COMPLETED) else r },
                    lastVerification = it.lastVerification?.copy(
                        reservation = it.lastVerification.reservation?.copy(status = ReservationStatus.COMPLETED)
                    )
                )
            }
            onDone()
        }
    }

    fun clearVerification() = _uiState.update { it.copy(lastVerification = null) }
}
