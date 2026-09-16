package com.solgrid.mobile.feature.prosumer

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import com.solgrid.mobile.core.components.AppTextField
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.ConfirmationDialog
import com.solgrid.mobile.core.components.PrimaryButton
import com.solgrid.mobile.core.components.SecondaryButton
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing

/** Update or cancel a reservation (MOB-09). The API enforces the 12-hour notice rule. */
@Composable
fun EditReservationScreen(
    viewModel: ProsumerViewModel,
    reservationId: String,
    onBack: () -> Unit,
    onUpdated: () -> Unit,
    onCancelled: () -> Unit
) {
    val colors = SolGridTheme.colors
    val reservation = viewModel.reservationById(reservationId)
    var date by remember { mutableStateOf(reservation?.date.orEmpty()) }
    var startTime by remember { mutableStateOf(reservation?.startTime.orEmpty()) }
    var endTime by remember { mutableStateOf(reservation?.endTime.orEmpty()) }
    var errorText by remember { mutableStateOf<String?>(null) }
    var showCancelConfirm by remember { mutableStateOf(false) }
    var submitting by remember { mutableStateOf(false) }

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Edit Reservation", onBack = onBack)
        Column(modifier = Modifier.fillMaxSize().padding(horizontal = Spacing.lg)) {
            if (reservation == null) {
                Text("Reservation not found.", style = AppType.body, color = colors.textSecondary, modifier = Modifier.padding(top = Spacing.xl))
                return@Column
            }

            Text(reservation.nodeName, style = AppType.sectionTitle, color = colors.textPrimary, modifier = Modifier.padding(top = Spacing.md))

            val modifyError = remember(reservation) { viewModel.validateModifyWindow(reservation) }
            if (modifyError != null) {
                Text(modifyError, style = AppType.caption, color = colors.error, modifier = Modifier.padding(top = Spacing.sm))
            }

            AppTextField(value = date, onValueChange = { date = it }, label = "Date", modifier = Modifier.padding(top = Spacing.xl), enabled = modifyError == null)
            AppTextField(value = startTime, onValueChange = { startTime = it }, label = "Start time", modifier = Modifier.padding(top = Spacing.md), enabled = modifyError == null)
            AppTextField(value = endTime, onValueChange = { endTime = it }, label = "End time", modifier = Modifier.padding(top = Spacing.md), enabled = modifyError == null)

            if (errorText != null) {
                Text(errorText!!, style = AppType.caption, color = colors.error, modifier = Modifier.padding(top = Spacing.sm))
            }

            PrimaryButton(
                text = "Save Changes",
                loading = submitting,
                enabled = modifyError == null,
                onClick = {
                    if (date.isBlank() || startTime.isBlank() || endTime.isBlank()) {
                        errorText = "All fields are required."
                        return@PrimaryButton
                    }
                    errorText = null
                    submitting = true
                    viewModel.updateReservation(reservation.id, date, startTime, endTime) {
                        submitting = false
                        onUpdated()
                    }
                },
                modifier = Modifier.fillMaxWidth().padding(top = Spacing.xl)
            )
            SecondaryButton(
                text = "Cancel Reservation",
                enabled = modifyError == null,
                onClick = { showCancelConfirm = true },
                modifier = Modifier.fillMaxWidth().padding(top = Spacing.md, bottom = Spacing.xxxl)
            )
        }
    }

    if (showCancelConfirm && reservation != null) {
        ConfirmationDialog(
            title = "Cancel this reservation?",
            message = "This will free up the slot for other prosumers. This action cannot be undone.",
            confirmText = "Cancel Reservation",
            destructive = true,
            onConfirm = {
                showCancelConfirm = false
                viewModel.cancelReservation(reservation.id, onCancelled)
            },
            onDismiss = { showCancelConfirm = false }
        )
    }
}
