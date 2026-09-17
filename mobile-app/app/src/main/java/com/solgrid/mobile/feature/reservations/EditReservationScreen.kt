package com.solgrid.mobile.feature.reservations
import com.solgrid.mobile.feature.prosumer.ProsumerViewModel

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.EventBusy
import androidx.compose.material.icons.outlined.Schedule
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.EmptyStateIcons
import com.solgrid.mobile.core.components.PrimaryButton
import com.solgrid.mobile.core.components.StatePlaceholder
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.feature.microgrid.NodeViewModel

/** Reschedule a booking to a different slot (MOB-09). The 12-hour notice rule is enforced by the
 * API — this screen only presents available slots and surfaces the returned outcome. */
@Composable
fun EditReservationScreen(
    viewModel: ProsumerViewModel,
    reservationId: String,
    onBack: () -> Unit,
    onUpdated: () -> Unit
) {
    val colors = SolGridTheme.colors
    val state by viewModel.uiState.collectAsStateWithLifecycle()
    val reservation = state.reservations.find { it.id == reservationId }
    val nodeViewModel = remember { NodeViewModel() }
    val nodeState by nodeViewModel.state.collectAsState()

    LaunchedEffect(reservation?.nodeId) { reservation?.nodeId?.let { nodeViewModel.loadDetail(it) } }

    var selectedSlotId by rememberSaveable(reservationId) { mutableStateOf<String?>(null) }
    var errorText by remember { mutableStateOf<String?>(null) }
    var submitting by remember { mutableStateOf(false) }

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Reschedule Booking", onBack = onBack)

        if (reservation == null) {
            StatePlaceholder(
                icon = EmptyStateIcons.NoActivity,
                title = "Booking not found",
                description = "This reservation is no longer available to edit.",
                modifier = Modifier.padding(top = Spacing.xxxl)
            )
            return@Column
        }

        val windowError = remember(reservation) { viewModel.validateModifyWindow(reservation) }
        // Keep the current slot in the list so the prosumer always sees where they stand.
        val slots = nodeState.slots.filter { it.isActive && (it.isAvailable || it.id == reservation.bookingSlotId) }

        Column(
            modifier = Modifier
                .weight(1f)
                .verticalScroll(rememberScrollState())
                .padding(horizontal = Spacing.lg),
            verticalArrangement = Arrangement.spacedBy(Spacing.md)
        ) {
            // Current booking recap
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(top = Spacing.md)
                    .clip(RoundedCornerShape(Radius.lg))
                    .background(colors.surfaceAlt)
                    .padding(Spacing.lg),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(Spacing.md)
            ) {
                Icon(Icons.Outlined.Schedule, contentDescription = null, tint = colors.textSecondary)
                Column(modifier = Modifier.weight(1f)) {
                    Text("Currently booked", style = AppType.caption, color = colors.textTertiary)
                    Text(reservation.nodeName, style = AppType.bodyStrong, color = colors.textPrimary, modifier = Modifier.padding(top = Spacing.xxs))
                    Text("${reservation.date} · ${reservation.startTime} – ${reservation.endTime}", style = AppType.supporting, color = colors.textSecondary)
                }
            }

            if (windowError != null) {
                Text(windowError, style = AppType.body, color = colors.error, modifier = Modifier.padding(top = Spacing.sm))
            }

            Text("Pick a new slot", style = AppType.sectionTitle, color = colors.textPrimary, modifier = Modifier.padding(top = Spacing.sm))

            if (slots.isEmpty()) {
                Row(
                    modifier = Modifier.fillMaxWidth().padding(vertical = Spacing.md),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(Spacing.sm)
                ) {
                    Icon(Icons.Outlined.EventBusy, contentDescription = null, tint = colors.textSecondary)
                    Text("No other slots are open right now.", style = AppType.body, color = colors.textSecondary)
                }
            } else {
                slots.forEach { slot ->
                    val isCurrent = slot.id == reservation.bookingSlotId
                    SlotSelectCard(
                        slot = slot,
                        selected = selectedSlotId == slot.id || (selectedSlotId == null && isCurrent),
                        enabled = windowError == null,
                        onClick = { selectedSlotId = slot.id; errorText = null }
                    )
                }
            }

            nodeState.error?.let { Text(it, style = AppType.caption, color = colors.error) }
            errorText?.let { Text(it, style = AppType.caption, color = colors.error) }

            Text(
                "Changes need to be at least 12 hours before the start time. Your booking returns to pending until a Grid Operator re-approves it.",
                style = AppType.caption,
                color = colors.textTertiary,
                modifier = Modifier.padding(top = Spacing.xs, bottom = Spacing.sm)
            )
        }

        PrimaryButton(
            text = "Save Changes",
            loading = submitting,
            enabled = windowError == null,
            onClick = {
                val newSlotId = selectedSlotId
                if (newSlotId == null || newSlotId == reservation.bookingSlotId) {
                    errorText = "Choose a different slot to reschedule."
                    return@PrimaryButton
                }
                val slot = slots.find { it.id == newSlotId } ?: return@PrimaryButton
                errorText = null
                submitting = true
                viewModel.updateReservation(
                    reservationId = reservation.id,
                    stationId = reservation.nodeId,
                    bookingSlotId = slot.id,
                    nodeName = reservation.nodeName,
                    scheduledAtIso = slot.startTime,
                    onError = { submitting = false; errorText = it },
                ) {
                    submitting = false
                    onUpdated()
                }
            },
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = Spacing.lg, vertical = Spacing.md)
        )
    }
}
