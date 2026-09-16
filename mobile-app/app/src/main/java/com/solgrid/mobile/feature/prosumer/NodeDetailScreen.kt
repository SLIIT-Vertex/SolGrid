package com.solgrid.mobile.feature.prosumer

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.PinDrop
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.components.AppDivider
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.BadgeTone
import com.solgrid.mobile.core.components.PrimaryButton
import com.solgrid.mobile.core.components.SectionHeader
import com.solgrid.mobile.core.components.StatTile
import com.solgrid.mobile.core.components.StatusBadge
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.mock.MockData
import com.solgrid.mobile.core.models.NodeStatus
import com.solgrid.mobile.core.models.SlotStatus

@Composable
fun NodeDetailScreen(nodeId: String, onBack: () -> Unit, onBookSlot: (String) -> Unit) {
    val colors = SolGridTheme.colors
    val node = MockData.nodes.find { it.id == nodeId } ?: MockData.nodes.first()
    val slots = MockData.slotsForNode(node.id)

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Node Details", onBack = onBack)
        Column(modifier = Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = Spacing.lg)) {
            Text(node.name, style = AppType.screenTitle, color = colors.textPrimary, modifier = Modifier.padding(top = Spacing.md))
            Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.padding(top = Spacing.xs)) {
                Icon(Icons.Outlined.PinDrop, contentDescription = null, tint = colors.textSecondary, modifier = Modifier.size(16.dp))
                Text(node.address, style = AppType.supporting, color = colors.textSecondary, modifier = Modifier.padding(start = Spacing.xs))
            }
            Row(modifier = Modifier.padding(top = Spacing.sm), horizontalArrangement = Arrangement.spacedBy(Spacing.sm)) {
                StatusBadge(
                    text = if (node.status == NodeStatus.ACTIVE) "Active" else "Inactive",
                    tone = if (node.status == NodeStatus.ACTIVE) BadgeTone.SUCCESS else BadgeTone.NEUTRAL
                )
                StatusBadge(text = "${node.capacityKw} kW capacity", tone = BadgeTone.NEUTRAL)
            }

            Row(
                modifier = Modifier.fillMaxWidth().padding(top = Spacing.xl),
                horizontalArrangement = Arrangement.spacedBy(Spacing.sm)
            ) {
                StatTile(value = "%.0f kW".format(node.capacityKw), label = "Capacity", modifier = Modifier.weight(1f))
                StatTile(value = "${node.availableSlots}/${node.totalSlots}", label = "Slots free", highlighted = true, modifier = Modifier.weight(1f))
                StatTile(value = "%.4f".format(node.latitude), label = "Latitude", modifier = Modifier.weight(1f))
            }

            SectionHeader(title = "Available Slots", modifier = Modifier.padding(top = Spacing.xl))
            Column {
                slots.forEach { slot ->
                    Row(
                        modifier = Modifier.fillMaxWidth().padding(vertical = Spacing.sm),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column {
                            Text(slot.date, style = AppType.bodyStrong, color = colors.textPrimary)
                            Text("${slot.startTime} – ${slot.endTime}", style = AppType.supporting, color = colors.textSecondary)
                        }
                        StatusBadge(
                            text = when (slot.status) {
                                SlotStatus.AVAILABLE -> "Available"
                                SlotStatus.RESERVED -> "Reserved"
                                SlotStatus.UNAVAILABLE -> "Unavailable"
                            },
                            tone = when (slot.status) {
                                SlotStatus.AVAILABLE -> BadgeTone.SUCCESS
                                SlotStatus.RESERVED -> BadgeTone.WARNING
                                SlotStatus.UNAVAILABLE -> BadgeTone.NEUTRAL
                            }
                        )
                    }
                    AppDivider()
                }
            }

            PrimaryButton(
                text = "Book a Slot at This Node",
                onClick = { onBookSlot(node.id) },
                enabled = node.status == NodeStatus.ACTIVE && node.availableSlots > 0,
                modifier = Modifier.fillMaxWidth().padding(top = Spacing.xl, bottom = Spacing.xxxl)
            )
        }
    }
}
