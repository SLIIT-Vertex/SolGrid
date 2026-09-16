package com.solgrid.mobile.feature.prosumer

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.ExpandMore
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.ConfirmationDialog
import com.solgrid.mobile.core.components.PrimaryButton
import com.solgrid.mobile.core.components.SecondaryButton
import com.solgrid.mobile.core.components.clickableNoRipple
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.network.BookingSlotDto
import com.solgrid.mobile.feature.microgrid.NodeViewModel
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter

private val slotLabelFormatter = DateTimeFormatter.ofPattern("MMM d, hh:mm a")

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
    val nodeViewModel = remember { NodeViewModel() }
    val nodeState by nodeViewModel.state.collectAsState()

    LaunchedEffect(reservation?.nodeId) {
        reservation?.nodeId?.let { nodeViewModel.loadDetail(it) }
    }

    var selectedSlotIndex by remember { mutableStateOf(-1) }
    var slotMenuExpanded by remember { mutableStateOf(false) }
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
            Text("Currently: ${reservation.date} · ${reservation.startTime}", style = AppType.supporting, color = colors.textSecondary)

            val modifyError = remember(reservation) { viewModel.validateModifyWindow(reservation) }
            if (modifyError != null) {
                Text(modifyError, style = AppType.caption, color = colors.error, modifier = Modifier.padding(top = Spacing.sm))
            }

            val availableSlots = nodeState.slots.filter { it.isActive && (it.isAvailable || it.id == reservation.bookingSlotId) }

            Text("Select a new slot", style = AppType.bodyStrong, color = colors.textPrimary, modifier = Modifier.padding(top = Spacing.xl))
            Box(modifier = Modifier.padding(top = Spacing.sm)) {
                val selected = availableSlots.getOrNull(selectedSlotIndex)
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .clip(RoundedCornerShape(Radius.md))
                        .background(colors.surface)
                        .padding(Spacing.md),
                    horizontalArrangement = androidx.compose.foundation.layout.Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = selected?.let(::slotLabel) ?: "Keep current slot",
                        style = AppType.body,
                        color = colors.textPrimary,
                        modifier = Modifier
                            .weight(1f)
                            .then(if (modifyError == null) Modifier.clickableNoRipple { slotMenuExpanded = true } else Modifier)
                    )
                    Icon(Icons.Outlined.ExpandMore, contentDescription = null, tint = colors.textSecondary)
                }
                DropdownMenu(expanded = slotMenuExpanded, onDismissRequest = { slotMenuExpanded = false }) {
                    availableSlots.forEachIndexed { index, slot ->
                        DropdownMenuItem(
                            text = { Text(slotLabel(slot)) },
                            onClick = { selectedSlotIndex = index; slotMenuExpanded = false }
                        )
                    }
                }
            }

            if (errorText != null) {
                Text(errorText!!, style = AppType.caption, color = colors.error, modifier = Modifier.padding(top = Spacing.sm))
            }

            PrimaryButton(
                text = "Save Changes",
                loading = submitting,
                enabled = modifyError == null,
                onClick = {
                    val slot = availableSlots.getOrNull(selectedSlotIndex)
                    if (slot == null) {
                        errorText = "Select a new slot to reschedule."
                        return@PrimaryButton
                    }
                    errorText = null
                    submitting = true
                    viewModel.updateReservation(
                        reservationId = reservation.id,
                        stationId = reservation.nodeId,
                        bookingSlotId = slot.id,
                        nodeName = reservation.nodeName,
                        scheduledAtIso = slot.startTime,
                        onError = {
                            submitting = false
                            errorText = it
                        },
                    ) {
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
                viewModel.cancelReservation(
                    reservation.id,
                    onError = { errorText = it },
                    onDone = onCancelled,
                )
            },
            onDismiss = { showCancelConfirm = false }
        )
    }
}

private fun slotLabel(slot: BookingSlotDto): String {
    val start = OffsetDateTime.parse(slot.startTime).atZoneSameInstant(ZoneId.systemDefault())
    val end = OffsetDateTime.parse(slot.endTime).atZoneSameInstant(ZoneId.systemDefault())
    return "${start.format(slotLabelFormatter)} – ${end.format(DateTimeFormatter.ofPattern("hh:mm a"))}"
}
