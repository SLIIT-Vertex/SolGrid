package com.solgrid.mobile.feature.operator

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Bolt
import androidx.compose.material.icons.outlined.ChevronRight
import androidx.compose.material3.Icon
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import com.solgrid.mobile.core.components.AppDivider
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.BadgeTone
import com.solgrid.mobile.core.components.EmptyStateIcons
import com.solgrid.mobile.core.components.ErrorKind
import com.solgrid.mobile.core.components.ErrorState
import com.solgrid.mobile.core.components.ListRow
import com.solgrid.mobile.core.components.ListRowSkeleton
import com.solgrid.mobile.core.components.SearchField
import com.solgrid.mobile.core.components.StatePlaceholder
import com.solgrid.mobile.core.components.StatusBadge
import com.solgrid.mobile.core.components.clickableNoRipple
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.network.SolarStationDto
import com.solgrid.mobile.feature.microgrid.NodeViewModel

/** Grid Operator's node browsing list — styled to match the rest of the app's list screens
 * (search field, ListRow entries, loading skeletons, shared empty/error states). */
@Composable
fun OperatorNodesScreen(viewModel: NodeViewModel, onBack: () -> Unit, onNodeClick: (String) -> Unit) {
    val state by viewModel.state.collectAsState()
    val colors = SolGridTheme.colors
    var search by remember { mutableStateOf("") }

    LaunchedEffect(Unit) { viewModel.loadOperatorNodes() }

    Column(Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Grid Nodes", onBack = onBack)
        Column(modifier = Modifier.padding(horizontal = Spacing.lg, vertical = Spacing.sm)) {
            SearchField(
                value = search,
                onValueChange = {
                    search = it
                    viewModel.loadOperatorNodes(search = it)
                },
                placeholder = "Search nodes"
            )
        }

        when {
            state.loading && state.nodes.isEmpty() -> Column(Modifier.padding(horizontal = Spacing.lg)) {
                repeat(5) { ListRowSkeleton() }
            }
            state.error != null -> ErrorState(
                ErrorKind.SERVER_ERROR,
                onRetry = { viewModel.loadOperatorNodes(search = search) }
            )
            state.nodes.isEmpty() -> StatePlaceholder(
                EmptyStateIcons.NoActivity,
                "No nodes found",
                "Try a different search term."
            )
            else -> LazyColumn(contentPadding = PaddingValues(horizontal = Spacing.lg)) {
                items(state.nodes, key = { it.id }) { node ->
                    NodeListItem(node = node, onClick = { onNodeClick(node.id) })
                    AppDivider()
                }
            }
        }
    }
}

@Composable
private fun NodeListItem(node: SolarStationDto, onClick: () -> Unit) {
    val colors = SolGridTheme.colors
    ListRow(
        title = node.name,
        subtitle = "${node.code} · ${node.availableSlotCount}/${node.totalSlotCount} slots available",
        icon = Icons.Outlined.Bolt,
        modifier = Modifier.clickableNoRipple(onClick),
        trailing = {
            Row(verticalAlignment = androidx.compose.ui.Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(Spacing.sm)) {
                StatusBadge(
                    text = if (node.status == 1) "Active" else "Inactive",
                    tone = if (node.status == 1) BadgeTone.SUCCESS else BadgeTone.NEUTRAL
                )
                Icon(Icons.Outlined.ChevronRight, contentDescription = null, tint = colors.textTertiary)
            }
        }
    )
}
