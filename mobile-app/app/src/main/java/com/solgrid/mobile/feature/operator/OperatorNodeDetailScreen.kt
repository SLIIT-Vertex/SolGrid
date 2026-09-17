package com.solgrid.mobile.feature.operator

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import com.solgrid.mobile.core.components.AppDivider
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.BadgeTone
import com.solgrid.mobile.core.components.EmptyStateIcons
import com.solgrid.mobile.core.components.ListRow
import com.solgrid.mobile.core.components.SecondaryButton
import com.solgrid.mobile.core.components.SectionHeader
import com.solgrid.mobile.core.components.StatePlaceholder
import com.solgrid.mobile.core.components.StatTile
import com.solgrid.mobile.core.components.StatusBadge
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.network.BookingSlotDto
import com.solgrid.mobile.core.network.ReservationDto
import com.solgrid.mobile.feature.microgrid.NodeViewModel
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter

private val dateTimeFormatter = DateTimeFormatter.ofPattern("MMM d, hh:mm a")
private val timeFormatter = DateTimeFormatter.ofPattern("hh:mm a")

/** Converts a backend UTC instant to the device's local time for display — these are real
 * timestamps, unlike the station's timezone-less weekly schedule clock strings. */
private fun formatInstant(iso: String): String = runCatching {
    OffsetDateTime.parse(iso).atZoneSameInstant(ZoneId.systemDefault()).format(dateTimeFormatter)
}.getOrDefault(iso)

private fun formatTimeRange(startIso: String, endIso: String): String = runCatching {
    val start = OffsetDateTime.parse(startIso).atZoneSameInstant(ZoneId.systemDefault())
    val end = OffsetDateTime.parse(endIso).atZoneSameInstant(ZoneId.systemDefault())
    "${start.format(dateTimeFormatter)} – ${end.format(timeFormatter)}"
}.getOrDefault("$startIso – $endIso")

@Composable
fun OperatorNodeDetailScreen(viewModel: NodeViewModel, nodeId: String, onBack: () -> Unit) {
    val state by viewModel.state.collectAsState()
    val colors = SolGridTheme.colors

    LaunchedEffect(nodeId) {
        viewModel.loadDetail(nodeId)
        viewModel.loadBookings(nodeId)
    }

    Column(Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Node monitoring", onBack = onBack)

        val node = state.selected
        when {
            node == null && state.loading -> Box(
                Modifier.fillMaxSize(),
                contentAlignment = Alignment.Center
            ) { CircularProgressIndicator(color = colors.accent) }
            node == null -> StatePlaceholder(
                EmptyStateIcons.NoActivity,
                "Node unavailable",
                state.error ?: "The node could not be loaded."
            )
            else -> Column(
                Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = Spacing.lg)
            ) {
                Row(
                    modifier = Modifier.fillMaxWidth().padding(top = Spacing.lg),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = androidx.compose.foundation.layout.Arrangement.spacedBy(Spacing.sm)
                ) {
                    Column(Modifier.weight(1f)) {
                        Text(node.name, style = AppType.screenTitle, color = colors.textPrimary)
                        Text(node.code, style = AppType.supporting, color = colors.textSecondary)
                    }
                    StatusBadge(
                        text = if (node.status == 1) "Active" else "Inactive",
                        tone = if (node.status == 1) BadgeTone.SUCCESS else BadgeTone.NEUTRAL
                    )
                }

                Row(
                    modifier = Modifier.fillMaxWidth().padding(top = Spacing.lg),
                    horizontalArrangement = androidx.compose.foundation.layout.Arrangement.spacedBy(Spacing.sm)
                ) {
                    StatTile(
                        value = "${node.capacityKw} kW",
                        label = "Generation capacity",
                        modifier = Modifier.weight(1f)
                    )
                    StatTile(
                        value = "${node.availableSlotCount}/${node.totalSlotCount}",
                        label = "Slots available",
                        modifier = Modifier.weight(1f),
                        highlighted = node.availableSlotCount > 0
                    )
                }

                SectionHeader(title = "Node bookings", modifier = Modifier.padding(top = Spacing.xl))
                if (state.bookings.isEmpty()) {
                    Text("No bookings found for this node.", style = AppType.body, color = colors.textSecondary)
                } else {
                    Column(
                        Modifier
                            .fillMaxWidth()
                            .clip(RoundedCornerShape(Radius.lg))
                            .background(colors.surface)
                    ) {
                        state.bookings.forEachIndexed { index, booking ->
                            BookingRow(booking)
                            if (index != state.bookings.lastIndex) AppDivider()
                        }
                    }
                }

                SectionHeader(title = "Battery slot availability", modifier = Modifier.padding(top = Spacing.xl))
                state.error?.let {
                    Text(it, color = colors.error, style = AppType.body, modifier = Modifier.padding(bottom = Spacing.sm))
                }
                if (state.slots.isEmpty()) {
                    Text("No battery slots configured for this node.", style = AppType.body, color = colors.textSecondary)
                } else {
                    Column(
                        Modifier
                            .fillMaxWidth()
                            .clip(RoundedCornerShape(Radius.lg))
                            .background(colors.surface)
                            .padding(vertical = Spacing.xs)
                    ) {
                        state.slots.forEachIndexed { index, slot ->
                            SlotRow(
                                slot = slot,
                                isChanging = state.changingSlot == slot.id,
                                onToggle = { viewModel.setSlotAvailability(nodeId, slot.id, !slot.isActive) }
                            )
                            if (index != state.slots.lastIndex) AppDivider()
                        }
                    }
                }

                androidx.compose.foundation.layout.Spacer(Modifier.padding(bottom = Spacing.xxxl))
            }
        }
    }
}

@Composable
private fun BookingRow(booking: ReservationDto) {
    ListRow(
        title = formatInstant(booking.scheduledAt),
        subtitle = "Slot ${booking.bookingSlotId}",
        trailing = { StatusBadge(text = reservationStatusLabel(booking.status), tone = reservationStatusTone(booking.status)) },
        modifier = Modifier.padding(horizontal = Spacing.md)
    )
}

@Composable
private fun SlotRow(slot: BookingSlotDto, isChanging: Boolean, onToggle: () -> Unit) {
    val colors = SolGridTheme.colors
    Column(Modifier.fillMaxWidth().padding(horizontal = Spacing.md, vertical = Spacing.md)) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = androidx.compose.foundation.layout.Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column {
                Text("Slot ${slot.slotNumber} · ${slot.batteryCapacityKwh} kWh", style = AppType.bodyStrong, color = colors.textPrimary)
                Text(formatTimeRange(slot.startTime, slot.endTime), style = AppType.supporting, color = colors.textSecondary)
            }
            StatusBadge(text = slotStatusLabel(slot.status), tone = slotStatusTone(slot.status))
        }
        if (slot.status == 1 || slot.status == 4) {
            SecondaryButton(
                text = if (slot.isActive) "Take out of service" else "Make available",
                enabled = !isChanging,
                onClick = onToggle,
                modifier = Modifier.padding(top = Spacing.sm)
            )
        }
    }
}

private fun reservationStatusLabel(value: Int) = listOf("Pending", "Approved", "Rejected", "Cancelled", "Completed").getOrElse(value - 1) { "Unknown" }

private fun reservationStatusTone(value: Int) = when (value) {
    1 -> BadgeTone.WARNING
    2 -> BadgeTone.SUCCESS
    3 -> BadgeTone.ERROR
    4 -> BadgeTone.NEUTRAL
    5 -> BadgeTone.ACCENT
    else -> BadgeTone.NEUTRAL
}

private fun slotStatusLabel(value: Int) = when (value) {
    1 -> "Available"
    2 -> "Reserved"
    3 -> "Occupied"
    4 -> "Out of service"
    else -> "Unknown"
}

private fun slotStatusTone(value: Int) = when (value) {
    1 -> BadgeTone.SUCCESS
    2, 3 -> BadgeTone.ACCENT
    4 -> BadgeTone.NEUTRAL
    else -> BadgeTone.NEUTRAL
}
