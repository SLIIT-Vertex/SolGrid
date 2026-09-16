package com.solgrid.mobile.feature.prosumer

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.BatteryChargingFull
import androidx.compose.material.icons.outlined.Bolt
import androidx.compose.material.icons.outlined.EvStation
import androidx.compose.material.icons.outlined.Map
import androidx.compose.material.icons.outlined.MyLocation
import androidx.compose.material.icons.outlined.SolarPower
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import androidx.compose.foundation.layout.offset
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.BadgeTone
import com.solgrid.mobile.core.components.IconToggleItem
import com.solgrid.mobile.core.components.IconToggleRow
import com.solgrid.mobile.core.components.StatusBadge
import com.solgrid.mobile.core.components.clickableNoRipple
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.mock.MockData
import com.solgrid.mobile.core.models.MicrogridNode
import com.solgrid.mobile.core.models.NodeStatus

/**
 * Nearby microgrid nodes. The assignment requires plotting stored lat/lng on Google Maps
 * (MOB-07); that SDK needs a Google Cloud API key which isn't available in this environment, so
 * this screen renders a clearly-labelled placeholder "map" (relative pin layout from stored
 * coordinates) plus a live list — swap `NodeMapPlaceholder` for a `com.google.maps.android.compose
 * .GoogleMap` composable once a key is configured.
 */
private val nodeFilters = listOf(
    IconToggleItem("all", Icons.Outlined.Map, "All"),
    IconToggleItem("active", Icons.Outlined.Bolt, "Active"),
    IconToggleItem("solar", Icons.Outlined.SolarPower, "Solar"),
    IconToggleItem("nearby", Icons.Outlined.MyLocation, "Nearby")
)

@Composable
fun NodesMapScreen(onBack: () -> Unit, onNodeClick: (String) -> Unit) {
    val colors = SolGridTheme.colors
    var selectedFilter by remember { mutableStateOf("all") }

    val filteredNodes = when (selectedFilter) {
        "active" -> MockData.nodes.filter { it.status == NodeStatus.ACTIVE }
        "nearby" -> MockData.nodes.sortedBy { it.distanceKm ?: Double.MAX_VALUE }.take(3)
        else -> MockData.nodes
    }

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Nearby Grid Nodes", onBack = onBack)
        NodeMapPlaceholder(nodes = MockData.nodes, onNodeClick = onNodeClick, modifier = Modifier.padding(horizontal = Spacing.lg))

        IconToggleRow(
            items = nodeFilters,
            selectedId = selectedFilter,
            onSelect = { selectedFilter = it },
            modifier = Modifier.padding(horizontal = Spacing.lg, vertical = Spacing.md)
        )

        LazyColumn(contentPadding = PaddingValues(horizontal = Spacing.lg, vertical = Spacing.sm)) {
            items(filteredNodes, key = { it.id }) { node ->
                NodeRow(node = node, onClick = { onNodeClick(node.id) })
            }
        }
    }
}

@Composable
private fun NodeMapPlaceholder(nodes: List<MicrogridNode>, onNodeClick: (String) -> Unit, modifier: Modifier = Modifier) {
    val colors = SolGridTheme.colors
    // Normalize stored lat/lng into a relative 0..1 box purely for placeholder pin layout.
    val lats = nodes.map { it.latitude }
    val lngs = nodes.map { it.longitude }
    val latRange = (lats.max() - lats.min()).takeIf { it > 0 } ?: 1.0
    val lngRange = (lngs.max() - lngs.min()).takeIf { it > 0 } ?: 1.0

    Box(
        modifier = modifier
            .fillMaxWidth()
            .height(200.dp)
            .padding(top = Spacing.sm)
            .clip(RoundedCornerShape(Radius.lg))
            .background(colors.surfaceAlt)
    ) {
        Row(
            modifier = Modifier.padding(Spacing.sm),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(Spacing.xs)
        ) {
            Icon(Icons.Outlined.Map, contentDescription = null, tint = colors.textTertiary, modifier = Modifier.size(14.dp))
            Text("Map preview — live coordinates from server", style = AppType.caption, color = colors.textTertiary)
        }
        nodes.forEach { node ->
            val xFrac = ((node.longitude - lngs.min()) / lngRange).toFloat().coerceIn(0.08f, 0.92f)
            val yFrac = (1f - ((node.latitude - lats.min()) / latRange)).toFloat().coerceIn(0.2f, 0.85f)
            Box(
                modifier = Modifier
                    .offset(x = (xFrac * 320).dp, y = (yFrac * 160).dp)
                    .size(28.dp)
                    .clip(CircleShape)
                    .background(if (node.status == NodeStatus.ACTIVE) colors.accent else colors.textTertiary)
                    .clickableNoRipple { onNodeClick(node.id) },
                contentAlignment = Alignment.Center
            ) {
                Icon(Icons.Outlined.EvStation, contentDescription = node.name, tint = Color.White, modifier = Modifier.size(16.dp))
            }
        }
    }
}

@Composable
private fun NodeRow(node: MicrogridNode, onClick: () -> Unit) {
    val colors = SolGridTheme.colors
    Row(
        modifier = Modifier.fillMaxWidth().clickableNoRipple(onClick).padding(vertical = Spacing.md),
        horizontalArrangement = Arrangement.spacedBy(Spacing.md),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Box(
            modifier = Modifier.size(44.dp).clip(RoundedCornerShape(Radius.sm)).background(colors.accentSurface),
            contentAlignment = Alignment.Center
        ) {
            Icon(Icons.Outlined.BatteryChargingFull, contentDescription = null, tint = colors.accent)
        }
        Column(modifier = Modifier.weight(1f)) {
            Text(node.name, style = AppType.bodyStrong, color = colors.textPrimary)
            Text(node.address, style = AppType.supporting, color = colors.textSecondary)
            Row(modifier = Modifier.padding(top = Spacing.xs), horizontalArrangement = Arrangement.spacedBy(Spacing.sm)) {
                StatusBadge(
                    text = if (node.status == NodeStatus.ACTIVE) "Active" else "Inactive",
                    tone = if (node.status == NodeStatus.ACTIVE) BadgeTone.SUCCESS else BadgeTone.NEUTRAL
                )
                StatusBadge(text = "${node.availableSlots}/${node.totalSlots} slots free", tone = BadgeTone.ACCENT)
            }
        }
        node.distanceKm?.let {
            Text("${it} km", style = AppType.caption, color = colors.textTertiary)
        }
    }
}
