package com.solgrid.mobile.feature.operator

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.EmptyStateIcons
import com.solgrid.mobile.core.components.StatePlaceholder
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.feature.microgrid.NodeViewModel

@Composable
fun OperatorNodeDetailScreen(viewModel: NodeViewModel, nodeId: String, onBack: () -> Unit) {
    val state by viewModel.state.collectAsState(); val colors = SolGridTheme.colors
    LaunchedEffect(nodeId) { viewModel.loadDetail(nodeId); viewModel.loadBookings(nodeId) }
    Column(Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar("Node monitoring", onBack = onBack)
        val node = state.selected
        if (node == null && !state.loading) StatePlaceholder(EmptyStateIcons.NoActivity, "Node unavailable", state.error ?: "The node could not be loaded.")
        if (node != null) Column(Modifier.verticalScroll(rememberScrollState()).padding(horizontal = Spacing.lg)) {
            Text(node.name, style = AppType.screenTitle); Text("${node.code} · ${node.capacityKw} kW", style = AppType.supporting, color = colors.textSecondary)
            Text("${node.availableSlotCount}/${node.totalSlotCount} slots available", style = AppType.body, modifier = Modifier.padding(top = Spacing.lg))
            Text("Node bookings", style = AppType.sectionTitle, modifier = Modifier.padding(top = Spacing.xl))
            if (state.bookings.isEmpty()) Text("No bookings found for this node.", style = AppType.body, color = colors.textSecondary)
            state.bookings.forEach { booking -> Text("${booking.scheduledAt} · ${status(booking.status)} · slot ${booking.bookingSlotId}", style = AppType.body, modifier = Modifier.padding(top = Spacing.xs)) }
            Text("Battery slot availability", style = AppType.sectionTitle, modifier = Modifier.padding(top = Spacing.xl))
            state.error?.let { Text(it, color = colors.error, style = AppType.body) }
            state.slots.forEach { slot ->
                Text("Slot ${slot.slotNumber} · ${slot.batteryCapacityKwh} kWh · ${slot.startTime} – ${slot.endTime}", style = AppType.body, modifier = Modifier.padding(top = Spacing.md))
                if (slot.status == 1 || slot.status == 4) com.solgrid.mobile.core.components.SecondaryButton(
                    text = if (slot.isActive) "Take out of service" else "Make available",
                    enabled = state.changingSlot == null,
                    onClick = { viewModel.setSlotAvailability(nodeId, slot.id, !slot.isActive) }
                ) else Text("Reserved or occupied", style = AppType.caption)
            }
        }
    }
}
private fun status(value: Int) = listOf("Pending", "Approved", "Rejected", "Cancelled", "Completed").getOrElse(value - 1) { "Unknown" }
