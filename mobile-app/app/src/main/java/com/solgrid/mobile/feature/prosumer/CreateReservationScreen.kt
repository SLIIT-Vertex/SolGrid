package com.solgrid.mobile.feature.prosumer

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.ExpandMore
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
import com.solgrid.mobile.core.components.PrimaryButton
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

/** Create a new reservation (MOB-08). Slots come live from the station; the API enforces the
 * 7-day window and duplicate-active-booking rule authoritatively. */
@Composable
fun CreateReservationScreen(
    viewModel: ProsumerViewModel,
    nodeId: String,
    onBack: () -> Unit,
    onCreated: (reservationId: String) -> Unit
) {
    val colors = SolGridTheme.colors
    val nodeViewModel = remember { NodeViewModel() }
    val nodeState by nodeViewModel.state.collectAsState()

    LaunchedEffect(nodeId) { nodeViewModel.loadDetail(nodeId) }

    val availableSlots = nodeState.slots.filter { it.isActive && it.isAvailable }
    var selectedSlotIndex by remember { mutableStateOf(0) }
    var slotMenuExpanded by remember { mutableStateOf(false) }
    var errorText by remember { mutableStateOf<String?>(null) }
    var submitting by remember { mutableStateOf(false) }

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Create Reservation", onBack = onBack)
        Column(modifier = Modifier.fillMaxSize().padding(horizontal = Spacing.lg)) {
            Text(
                nodeState.selected?.name ?: "Loading station…",
                style = AppType.sectionTitle,
                color = colors.textPrimary,
                modifier = Modifier.padding(top = Spacing.md)
            )
            Text(nodeState.selected?.addressLine.orEmpty(), style = AppType.supporting, color = colors.textSecondary)

            Text("Select an available slot", style = AppType.bodyStrong, color = colors.textPrimary, modifier = Modifier.padding(top = Spacing.xl))
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
                        text = selected?.let(::slotLabel) ?: "No slots available",
                        style = AppType.body,
                        color = colors.textPrimary,
                        modifier = Modifier
                            .weight(1f)
                            .then(if (availableSlots.isNotEmpty()) Modifier.clickableNoRipple { slotMenuExpanded = true } else Modifier)
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
            nodeState.error?.let {
                Text(it, style = AppType.caption, color = colors.error, modifier = Modifier.padding(top = Spacing.sm))
            }

            Text(
                "Reservations can only be scheduled within the next 7 days. You can modify or cancel this booking up to 12 hours before its start time.",
                style = AppType.caption,
                color = colors.textTertiary,
                modifier = Modifier.padding(top = Spacing.lg)
            )

            PrimaryButton(
                text = "Confirm Reservation",
                loading = submitting,
                enabled = availableSlots.isNotEmpty(),
                onClick = {
                    val slot = availableSlots.getOrNull(selectedSlotIndex)
                    if (slot == null) {
                        errorText = "Please select a slot."
                        return@PrimaryButton
                    }
                    errorText = null
                    submitting = true
                    viewModel.createReservation(
                        stationId = nodeId,
                        bookingSlotId = slot.id,
                        nodeName = nodeState.selected?.name ?: nodeId,
                        scheduledAtIso = slot.startTime,
                        onError = {
                            submitting = false
                            errorText = it
                        },
                    ) { reservation ->
                        submitting = false
                        onCreated(reservation.id)
                    }
                },
                modifier = Modifier.fillMaxWidth().padding(top = Spacing.xl, bottom = Spacing.xxxl)
            )
        }
    }
}

private fun slotLabel(slot: BookingSlotDto): String {
    val start = OffsetDateTime.parse(slot.startTime).atZoneSameInstant(ZoneId.systemDefault())
    val end = OffsetDateTime.parse(slot.endTime).atZoneSameInstant(ZoneId.systemDefault())
    return "${start.format(slotLabelFormatter)} – ${end.format(DateTimeFormatter.ofPattern("hh:mm a"))}"
}
