package com.solgrid.mobile.feature.prosumer

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.ListItem
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.unit.dp
import com.google.android.gms.maps.model.CameraPosition
import com.google.android.gms.maps.model.LatLng
import com.google.maps.android.compose.GoogleMap
import com.google.maps.android.compose.Marker
import com.google.maps.android.compose.MarkerState
import com.google.maps.android.compose.rememberCameraPositionState
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.ErrorKind
import com.solgrid.mobile.core.components.ErrorState
import com.solgrid.mobile.core.components.StatePlaceholder
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.components.clickableNoRipple
import com.solgrid.mobile.feature.microgrid.NodeViewModel

@Composable
fun NodesMapScreen(viewModel: NodeViewModel, onBack: () -> Unit, onNodeClick: (String) -> Unit) {
    val state by viewModel.state.collectAsState()
    val colors = SolGridTheme.colors
    LaunchedEffect(Unit) { viewModel.loadNearby() }
    Column(Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Nearby Grid Nodes", onBack = onBack)
        when {
            state.error != null -> ErrorState(ErrorKind.SERVER_ERROR, onRetry = { viewModel.loadNearby() })
            state.nodes.isEmpty() && !state.loading -> StatePlaceholder(com.solgrid.mobile.core.components.EmptyStateIcons.NoActivity, "No nearby nodes", "Try again from another area.", actionText = "Retry", onAction = { viewModel.loadNearby() })
            else -> {
                if (state.nodes.isNotEmpty()) {
                    val camera = rememberCameraPositionState { position = CameraPosition.fromLatLngZoom(LatLng(state.nodes.first().location.latitude, state.nodes.first().location.longitude), 12f) }
                    GoogleMap(Modifier.fillMaxWidth().height(260.dp).padding(horizontal = Spacing.lg).clip(RoundedCornerShape(Radius.lg)), cameraPositionState = camera) {
                        state.nodes.forEach { node -> Marker(MarkerState(LatLng(node.location.latitude, node.location.longitude)), title = node.name, snippet = "${node.availableSlotCount} slots available", onClick = { onNodeClick(node.id); true }) }
                    }
                }
                Text("Results near Colombo", style = AppType.caption, color = colors.textSecondary, modifier = Modifier.padding(Spacing.lg))
                LazyColumn(contentPadding = PaddingValues(horizontal = Spacing.lg)) { items(state.nodes, key = { it.id }) { node -> ListItem(headlineContent = { Text(node.name) }, supportingContent = { Text("${node.addressLine} · ${node.availableSlotCount}/${node.totalSlotCount} slots") }, modifier = Modifier.clickableNoRipple { onNodeClick(node.id) }) } }
            }
        }
    }
}
