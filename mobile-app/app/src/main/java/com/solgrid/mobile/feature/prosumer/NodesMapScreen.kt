package com.solgrid.mobile.feature.prosumer

import android.Manifest
import android.app.Activity
import android.content.pm.PackageManager
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.MyLocation
import androidx.compose.material.icons.outlined.Search
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ListItem
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import com.google.android.gms.maps.model.CameraPosition
import com.google.android.gms.maps.model.LatLng
import com.google.android.libraries.places.api.Places
import com.google.android.libraries.places.api.model.Place
import com.google.android.libraries.places.widget.Autocomplete
import com.google.android.libraries.places.widget.model.AutocompleteActivityMode
import com.google.maps.android.compose.GoogleMap
import com.google.maps.android.compose.Marker
import com.google.maps.android.compose.MarkerState
import com.google.maps.android.compose.rememberCameraPositionState
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.ErrorKind
import com.solgrid.mobile.core.components.ErrorState
import com.solgrid.mobile.core.components.StatePlaceholder
import com.solgrid.mobile.core.components.clickableNoRipple
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.location.LocationHelper
import com.solgrid.mobile.feature.microgrid.NodeViewModel
import kotlinx.coroutines.launch

@Composable
fun NodesMapScreen(viewModel: NodeViewModel, onBack: () -> Unit, onNodeClick: (String) -> Unit) {
    val state by viewModel.state.collectAsState()
    val colors = SolGridTheme.colors
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    val locationHelper = remember { LocationHelper(context) }

    fun loadCurrentLocation() {
        scope.launch {
            val location = locationHelper.getCurrentLocation()
            if (location != null) {
                viewModel.loadNearby(location.first, location.second, areaLabel = "your current location")
            } else {
                viewModel.loadNearby(6.9271, 79.8612, areaLabel = "Colombo (location unavailable)")
            }
        }
    }

    val locationPermissionLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { granted ->
        if (granted) {
            loadCurrentLocation()
        } else {
            viewModel.loadNearby(6.9271, 79.8612, areaLabel = "Colombo (location permission denied)")
        }
    }

    fun requestLocation() {
        val hasPermission = androidx.core.content.ContextCompat.checkSelfPermission(
            context,
            Manifest.permission.ACCESS_FINE_LOCATION
        ) == PackageManager.PERMISSION_GRANTED
        if (hasPermission) {
            loadCurrentLocation()
        } else {
            locationPermissionLauncher.launch(Manifest.permission.ACCESS_FINE_LOCATION)
        }
    }

    val searchAreaLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.StartActivityForResult()
    ) { result ->
        if (result.resultCode == Activity.RESULT_OK && result.data != null) {
            val place = Autocomplete.getPlaceFromIntent(result.data!!)
            val latLng = place.latLng
            if (latLng != null) {
                viewModel.loadNearby(latLng.latitude, latLng.longitude, areaLabel = place.displayName ?: place.formattedAddress)
            }
        }
    }

    fun launchAreaSearch() {
        if (!Places.isInitialized()) return
        val fields = listOf(Place.Field.ID, Place.Field.DISPLAY_NAME, Place.Field.FORMATTED_ADDRESS, Place.Field.LAT_LNG)
        val intent = Autocomplete.IntentBuilder(AutocompleteActivityMode.FULLSCREEN, fields).build(context)
        searchAreaLauncher.launch(intent)
    }

    LaunchedEffect(Unit) { requestLocation() }

    Column(Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(
            title = "Nearby Grid Nodes",
            onBack = onBack,
            actions = {
                IconButton(onClick = { launchAreaSearch() }) {
                    Icon(Icons.Outlined.Search, contentDescription = "Search an area", tint = colors.textPrimary)
                }
                IconButton(onClick = { requestLocation() }) {
                    Icon(Icons.Outlined.MyLocation, contentDescription = "Use my location", tint = colors.textPrimary)
                }
            }
        )
        when {
            state.error != null -> ErrorState(ErrorKind.SERVER_ERROR, onRetry = { requestLocation() })
            state.nodes.isEmpty() && !state.loading -> StatePlaceholder(
                com.solgrid.mobile.core.components.EmptyStateIcons.NoActivity,
                "No nearby nodes",
                "Try searching a different area.",
                actionText = "Search area",
                onAction = { launchAreaSearch() }
            )
            else -> {
                if (state.nodes.isNotEmpty()) {
                    val camera = rememberCameraPositionState { position = CameraPosition.fromLatLngZoom(LatLng(state.nodes.first().location.latitude, state.nodes.first().location.longitude), 12f) }
                    GoogleMap(Modifier.fillMaxWidth().height(260.dp).padding(horizontal = Spacing.lg).clip(RoundedCornerShape(Radius.lg)), cameraPositionState = camera) {
                        state.nodes.forEach { node -> Marker(MarkerState(LatLng(node.location.latitude, node.location.longitude)), title = node.name, snippet = "${node.availableSlotCount} slots available", onClick = { onNodeClick(node.id); true }) }
                    }
                }
                Row(Modifier.fillMaxWidth().padding(horizontal = Spacing.lg, vertical = Spacing.sm)) {
                    Text(
                        state.searchAreaLabel?.let { "Results near $it" } ?: "Results",
                        style = AppType.caption,
                        color = colors.textSecondary
                    )
                }
                LazyColumn(contentPadding = PaddingValues(horizontal = Spacing.lg)) { items(state.nodes, key = { it.id }) { node -> ListItem(headlineContent = { Text(node.name) }, supportingContent = { Text("${node.addressLine} · ${node.availableSlotCount}/${node.totalSlotCount} slots") }, modifier = Modifier.clickableNoRipple { onNodeClick(node.id) }) } }
            }
        }
    }
}
