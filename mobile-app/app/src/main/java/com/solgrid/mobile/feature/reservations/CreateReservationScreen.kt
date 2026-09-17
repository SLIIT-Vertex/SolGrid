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
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.EventBusy
import androidx.compose.material.icons.outlined.Info
import androidx.compose.material.icons.outlined.Place
import androidx.compose.material3.CircularProgressIndicator
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
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.BadgeTone
import com.solgrid.mobile.core.components.PrimaryButton
import com.solgrid.mobile.core.components.StatTile
import com.solgrid.mobile.core.components.StatusBadge
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.feature.microgrid.NodeViewModel

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

    val node = nodeState.selected
    // Show every real slot so reserved/occupied ones are visible but locked; only available ones book.
    val slots = remember(nodeState.slots) { nodeState.slots.filter { it.isActive } }
    var selectedSlotId by rememberSaveable(nodeId) { mutableStateOf<String?>(null) }
    val selectedSlot = slots.find { it.id == selectedSlotId && it.isAvailable }
    var errorText by remember { mutableStateOf<String?>(null) }
    var submitting by remember { mutableStateOf(false) }

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Book a Slot", onBack = onBack)

        if (node == null && nodeState.loading) {
            Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                CircularProgressIndicator(color = colors.accent)
            }
            return@Column
        }

        Column(
            modifier = Modifier
                .weight(1f)
                .verticalScroll(rememberScrollState())
                .padding(horizontal = Spacing.lg),
            verticalArrangement = Arrangement.spacedBy(Spacing.md)
        ) {
            // ---- Station header ----
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(top = Spacing.md)
                    .clip(RoundedCornerShape(Radius.lg))
                    .background(colors.surface)
                    .padding(Spacing.lg)
            ) {
                Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(Spacing.sm)) {
                    Column(modifier = Modifier.weight(1f)) {
                        Text(node?.name ?: "Loading station…", style = AppType.sectionTitle, color = colors.textPrimary)
                        node?.code?.let { Text(it, style = AppType.caption, color = colors.textTertiary, modifier = Modifier.padding(top = Spacing.xxs)) }
                    }
                    node?.let {
                        StatusBadge(
                            text = if (it.status == 1) "Active" else "Inactive",
                            tone = if (it.status == 1) BadgeTone.SUCCESS else BadgeTone.NEUTRAL
                        )
                    }
                }
                node?.addressLine?.takeIf { it.isNotBlank() }?.let { address ->
                    Row(
                        modifier = Modifier.padding(top = Spacing.sm),
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(Spacing.xs)
                    ) {
                        Icon(Icons.Outlined.Place, contentDescription = null, tint = colors.textSecondary, modifier = Modifier.size(16.dp))
                        Text(address, style = AppType.supporting, color = colors.textSecondary)
                    }
                }
                node?.let {
                    Row(
                        modifier = Modifier.fillMaxWidth().padding(top = Spacing.md),
                        horizontalArrangement = Arrangement.spacedBy(Spacing.sm)
                    ) {
                        StatTile(value = "${it.capacityKw} kW", label = "Capacity", modifier = Modifier.weight(1f))
                        StatTile(
                            value = "${it.availableSlotCount}/${it.totalSlotCount}",
                            label = "Slots open",
                            highlighted = it.availableSlotCount > 0,
                            modifier = Modifier.weight(1f)
                        )
                    }
                }
            }

            Text("Choose a time slot", style = AppType.sectionTitle, color = colors.textPrimary, modifier = Modifier.padding(top = Spacing.sm))
            Text("Reserved slots are locked. Pick an open one below.", style = AppType.supporting, color = colors.textSecondary)

            if (slots.isEmpty()) {
                Row(
                    modifier = Modifier.fillMaxWidth().padding(vertical = Spacing.lg),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(Spacing.sm)
                ) {
                    Icon(Icons.Outlined.EventBusy, contentDescription = null, tint = colors.textSecondary)
                    Text("No slots are configured for this station yet.", style = AppType.body, color = colors.textSecondary)
                }
            } else {
                slots.forEach { slot ->
                    SlotSelectCard(
                        slot = slot,
                        selected = selectedSlotId == slot.id && slot.isAvailable,
                        enabled = slot.isAvailable,
                        onClick = { selectedSlotId = slot.id; errorText = null }
                    )
                }
            }

            // ---- Review ----
            selectedSlot?.let { slot ->
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .clip(RoundedCornerShape(Radius.lg))
                        .background(colors.accentSurface)
                        .padding(Spacing.lg)
                ) {
                    Text("Your request", style = AppType.caption, color = colors.accent)
                    Text("${slotDayLabel(slot)} · ${slotTimeLabel(slot)}", style = AppType.bodyStrong, color = colors.textPrimary, modifier = Modifier.padding(top = Spacing.xs))
                    Text("Slot ${slot.slotNumber} · ${slot.batteryCapacityKwh.toInt()} kWh battery", style = AppType.supporting, color = colors.textSecondary, modifier = Modifier.padding(top = Spacing.xxs))
                }
            }

            errorText?.let { Text(it, style = AppType.caption, color = colors.error) }
            nodeState.error?.let { Text(it, style = AppType.caption, color = colors.error) }

            // ---- Rules hint ----
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(top = Spacing.xs, bottom = Spacing.sm)
                    .clip(RoundedCornerShape(Radius.md))
                    .background(colors.surfaceAlt)
                    .padding(Spacing.md),
                horizontalArrangement = Arrangement.spacedBy(Spacing.sm)
            ) {
                Icon(Icons.Outlined.Info, contentDescription = null, tint = colors.textSecondary, modifier = Modifier.size(18.dp))
                Text(
                    "Bookings must be within the next 7 days. After a Grid Operator approves it, your secure QR appears in the booking details. You can change or cancel up to 12 hours before the start.",
                    style = AppType.caption,
                    color = colors.textSecondary
                )
            }
        }

        PrimaryButton(
            text = "Request reservation",
            loading = submitting,
            enabled = selectedSlot != null,
            onClick = {
                val slot = selectedSlot ?: run { errorText = "Please choose an open slot."; return@PrimaryButton }
                errorText = null
                submitting = true
                viewModel.createReservation(
                    stationId = nodeId,
                    bookingSlotId = slot.id,
                    nodeName = node?.name ?: nodeId,
                    scheduledAtIso = slot.startTime,
                    onError = { submitting = false; errorText = it },
                ) { reservation ->
                    submitting = false
                    onCreated(reservation.id)
                }
            },
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = Spacing.lg, vertical = Spacing.md)
        )
    }
}
