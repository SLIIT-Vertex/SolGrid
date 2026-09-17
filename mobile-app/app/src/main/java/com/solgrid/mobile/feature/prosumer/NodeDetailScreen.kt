package com.solgrid.mobile.feature.prosumer

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
import com.solgrid.mobile.core.components.PrimaryButton
import com.solgrid.mobile.core.components.StatePlaceholder
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.feature.microgrid.NodeViewModel

@Composable
fun NodeDetailScreen(viewModel: NodeViewModel, nodeId: String, onBack: () -> Unit, onBookSlot: (String) -> Unit) {
    val state by viewModel.state.collectAsState(); val colors = SolGridTheme.colors
    LaunchedEffect(nodeId) { viewModel.loadDetail(nodeId) }
    Column(Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar("Node details", onBack = onBack)
        val node = state.selected
        if (node == null && !state.loading) { StatePlaceholder(com.solgrid.mobile.core.components.EmptyStateIcons.NoActivity, "Node unavailable", state.error ?: "The node could not be found."); return@Column }
        if (node != null) Column(Modifier.verticalScroll(rememberScrollState()).padding(horizontal = Spacing.lg)) {
            Text(node.name, style = AppType.screenTitle, color = colors.textPrimary)
            Text("${node.code} · ${node.addressLine}", style = AppType.supporting, color = colors.textSecondary)
            Text("${if (node.status == 1) "Active" else "Inactive"} · ${node.capacityKw} kW", style = AppType.bodyStrong, modifier = Modifier.padding(top = Spacing.lg))
            Text("Coordinates: ${node.location.latitude}, ${node.location.longitude}", style = AppType.caption, color = colors.textSecondary)
            Text("Availability", style = AppType.sectionTitle, modifier = Modifier.padding(top = Spacing.xl))
            Text("${node.availableSlotCount} of ${node.totalSlotCount} booking slots available", style = AppType.body)
            Text("Weekly schedule", style = AppType.sectionTitle, modifier = Modifier.padding(top = Spacing.xl))
            if (node.schedule.isEmpty()) Text("No operating hours have been published.", style = AppType.body, color = colors.textSecondary)
            node.schedule.forEach { window -> Text("${java.time.DayOfWeek.of(if (window.day == 0) 7 else window.day)}: ${window.opensAt}–${window.closesAt}", style = AppType.body) }
            Text("Clock hours are supplied by the station schedule; no timezone is returned.", style = AppType.caption, color = colors.textSecondary)
            Text("Booking slots", style = AppType.sectionTitle, modifier = Modifier.padding(top = Spacing.xl))
            state.slots.forEach { slot -> Text("#${slot.slotNumber} · ${slot.startTime}–${slot.endTime} · ${slot.batteryCapacityKwh} kWh · ${slotStatus(slot.status, slot.isAvailable)}", style = AppType.body, modifier = Modifier.padding(vertical = Spacing.xs)) }
            PrimaryButton("Book a slot", enabled = node.status == 1 && node.availableSlotCount > 0, onClick = { onBookSlot(node.id) }, modifier = Modifier.padding(top = Spacing.xl, bottom = Spacing.xxxl))
        }
    }
}

private fun slotStatus(status: Int, available: Boolean) = when (status) { 1 -> if (available) "Available" else "Unavailable"; 2 -> "Reserved"; 3 -> "Occupied"; 4 -> "Out of service"; else -> "Unknown" }
