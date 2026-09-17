package com.solgrid.mobile.feature.operator

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.ListItem
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.ErrorKind
import com.solgrid.mobile.core.components.ErrorState
import com.solgrid.mobile.core.components.SearchField
import com.solgrid.mobile.core.components.StatePlaceholder
import com.solgrid.mobile.core.components.clickableNoRipple
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.feature.microgrid.NodeViewModel

@Composable
fun OperatorNodesScreen(viewModel: NodeViewModel, onBack: () -> Unit, onNodeClick: (String) -> Unit) {
    val state by viewModel.state.collectAsState(); val colors = SolGridTheme.colors
    var search by remember { mutableStateOf("") }
    LaunchedEffect(Unit) { viewModel.loadOperatorNodes() }
    Column(Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar("Grid nodes", onBack = onBack)
        SearchField(search, onValueChange = { search = it; viewModel.loadOperatorNodes(search = it) }, placeholder = "Search nodes", modifier = Modifier.padding(horizontal = Spacing.lg))
        when { state.error != null -> ErrorState(ErrorKind.SERVER_ERROR, onRetry = { viewModel.loadOperatorNodes(search = search) }); state.nodes.isEmpty() && !state.loading -> StatePlaceholder(com.solgrid.mobile.core.components.EmptyStateIcons.NoActivity, "No nodes found", "Try another filter."); else -> LazyColumn { items(state.nodes, key = { it.id }) { node -> ListItem(headlineContent = { Text(node.name) }, supportingContent = { Text("${node.code} · ${node.availableSlotCount}/${node.totalSlotCount} available") }, modifier = Modifier.clickableNoRipple { onNodeClick(node.id) }) } } }
    }
}
