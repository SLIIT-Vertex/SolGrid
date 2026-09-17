package com.solgrid.mobile.feature.prosumer

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
import com.solgrid.mobile.core.components.PrimaryButton
import com.solgrid.mobile.core.components.SectionHeader
import com.solgrid.mobile.core.components.StatePlaceholder
import com.solgrid.mobile.core.components.StatTile
import com.solgrid.mobile.core.components.StatusBadge
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.network.BookingSlotDto
import com.solgrid.mobile.core.network.OperatingWindowDto
import com.solgrid.mobile.feature.microgrid.NodeViewModel
import java.time.DayOfWeek
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter

private val dateTimeFormatter = DateTimeFormatter.ofPattern("MMM d, hh:mm a")
private val timeFormatter = DateTimeFormatter.ofPattern("hh:mm a")

/** Converts a backend UTC instant to the device's local time for display — these are real
 * timestamps, unlike the station's timezone-less weekly schedule clock strings below. */
private fun formatTimeRange(startIso: String, endIso: String): String = runCatching {
    val start = OffsetDateTime.parse(startIso).atZoneSameInstant(ZoneId.systemDefault())
    val end = OffsetDateTime.parse(endIso).atZoneSameInstant(ZoneId.systemDefault())
    "${start.format(dateTimeFormatter)} – ${end.format(timeFormatter)}"
}.getOrDefault("$startIso – $endIso")

@Composable
fun NodeDetailScreen(viewModel: NodeViewModel, nodeId: String, onBack: () -> Unit, onBookSlot: (String) -> Unit) {
    val state by viewModel.state.collectAsState()
    val colors = SolGridTheme.colors

    LaunchedEffect(nodeId) { viewModel.loadDetail(nodeId) }

    Column(Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Node details", onBack = onBack)

        val node = state.selected
        when {
            node == null && state.loading -> Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                CircularProgressIndicator(color = colors.accent)
            }
            node == null -> StatePlaceholder(
                EmptyStateIcons.NoActivity,
                "Node unavailable",
                state.error ?: "The node could not be found."
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
                Text(node.addressLine, style = AppType.body, color = colors.textSecondary, modifier = Modifier.padding(top = Spacing.xs))

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

                SectionHeader(title = "Weekly schedule", modifier = Modifier.padding(top = Spacing.xl))
                if (node.schedule.isEmpty()) {
                    Text("No operating hours have been published.", style = AppType.body, color = colors.textSecondary)
                } else {
                    Column(
                        Modifier
                            .fillMaxWidth()
                            .clip(RoundedCornerShape(Radius.lg))
                            .background(colors.surface)
                            .padding(vertical = Spacing.xs)
                    ) {
                        node.schedule.forEachIndexed { index, window ->
                            ScheduleRow(window)
                            if (index != node.schedule.lastIndex) AppDivider()
                        }
                    }
                    Text(
                        "Clock hours are supplied by the station schedule; no timezone is returned.",
                        style = AppType.caption,
                        color = colors.textTertiary,
                        modifier = Modifier.padding(top = Spacing.xs)
                    )
                }

                SectionHeader(title = "Booking slots", modifier = Modifier.padding(top = Spacing.xl))
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
                            SlotRow(slot)
                            if (index != state.slots.lastIndex) AppDivider()
                        }
                    }
                }

                PrimaryButton(
                    text = "Book a slot",
                    enabled = node.status == 1 && node.availableSlotCount > 0,
                    onClick = { onBookSlot(node.id) },
                    modifier = Modifier.fillMaxWidth().padding(top = Spacing.xl, bottom = Spacing.xxxl)
                )
            }
        }
    }
}

@Composable
private fun ScheduleRow(window: OperatingWindowDto) {
    val colors = SolGridTheme.colors
    val dayLabel = DayOfWeek.of(if (window.day == 0) 7 else window.day)
        .getDisplayName(java.time.format.TextStyle.FULL, java.util.Locale.getDefault())
    Row(
        modifier = Modifier.fillMaxWidth().padding(horizontal = Spacing.md, vertical = Spacing.md),
        horizontalArrangement = androidx.compose.foundation.layout.Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(dayLabel, style = AppType.bodyStrong, color = colors.textPrimary)
        Text("${window.opensAt} – ${window.closesAt}", style = AppType.body, color = colors.textSecondary)
    }
}

@Composable
private fun SlotRow(slot: BookingSlotDto) {
    Column(Modifier.fillMaxWidth().padding(horizontal = Spacing.md, vertical = Spacing.md)) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = androidx.compose.foundation.layout.Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column {
                Text("Slot ${slot.slotNumber} · ${slot.batteryCapacityKwh} kWh", style = AppType.bodyStrong, color = SolGridTheme.colors.textPrimary)
                Text(formatTimeRange(slot.startTime, slot.endTime), style = AppType.supporting, color = SolGridTheme.colors.textSecondary)
            }
            StatusBadge(text = slotStatusLabel(slot.status, slot.isAvailable), tone = slotStatusTone(slot.status, slot.isAvailable))
        }
    }
}

private fun slotStatusLabel(status: Int, available: Boolean) = when (status) {
    1 -> if (available) "Available" else "Unavailable"
    2 -> "Reserved"
    3 -> "Occupied"
    4 -> "Out of service"
    else -> "Unknown"
}

private fun slotStatusTone(status: Int, available: Boolean) = when (status) {
    1 -> if (available) BadgeTone.SUCCESS else BadgeTone.NEUTRAL
    2, 3 -> BadgeTone.ACCENT
    4 -> BadgeTone.NEUTRAL
    else -> BadgeTone.NEUTRAL
}
