package com.solgrid.mobile.feature.prosumer

import android.Manifest
import android.content.pm.PackageManager
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.ArrowBack
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.outlined.ChevronRight
import androidx.compose.material.icons.outlined.LocationOn
import androidx.compose.material.icons.outlined.MyLocation
import androidx.compose.material.icons.outlined.Search
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import com.google.android.gms.maps.CameraUpdateFactory
import com.google.android.gms.maps.model.BitmapDescriptorFactory
import com.google.android.gms.maps.model.CameraPosition
import com.google.android.gms.maps.model.LatLng
import com.google.android.gms.maps.model.LatLngBounds
import com.google.android.libraries.places.api.Places
import com.google.android.libraries.places.api.model.AutocompletePrediction
import com.google.android.libraries.places.api.model.AutocompleteSessionToken
import com.google.android.libraries.places.api.model.Place
import com.google.android.libraries.places.api.net.FetchPlaceRequest
import com.google.android.libraries.places.api.net.FindAutocompletePredictionsRequest
import com.google.maps.android.compose.GoogleMap
import com.google.maps.android.compose.MapUiSettings
import com.google.maps.android.compose.Marker
import com.google.maps.android.compose.MarkerState
import com.google.maps.android.compose.rememberCameraPositionState
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
import kotlinx.coroutines.tasks.await
import java.util.UUID

/** Caps how far `newLatLngBounds` is allowed to zoom in for a single (or tightly clustered) node —
 * without this, a lone result zooms all the way to street level instead of a useful area view. */
private const val MAX_AUTO_ZOOM = 14f

private val slotTimeFormatter = java.time.format.DateTimeFormatter.ofPattern("MMM d, hh:mm a")
private val slotEndTimeFormatter = java.time.format.DateTimeFormatter.ofPattern("hh:mm a")

/** Converts a backend UTC instant to the device's local time for display. */
private fun formatSlotTimeRange(startIso: String, endIso: String): String = runCatching {
    val start = java.time.OffsetDateTime.parse(startIso).atZoneSameInstant(java.time.ZoneId.systemDefault())
    val end = java.time.OffsetDateTime.parse(endIso).atZoneSameInstant(java.time.ZoneId.systemDefault())
    "${start.format(slotTimeFormatter)} – ${end.format(slotEndTimeFormatter)}"
}.getOrDefault("$startIso – $endIso")

private fun slotStatusLabel(status: Int, available: Boolean) = when (status) {
    1 -> if (available) "Available" else "Unavailable"
    2 -> "Reserved"
    3 -> "Occupied"
    4 -> "Out of service"
    else -> "Unknown"
}

private fun slotStatusTone(status: Int, available: Boolean) = when (status) {
    1 -> if (available) com.solgrid.mobile.core.components.BadgeTone.SUCCESS else com.solgrid.mobile.core.components.BadgeTone.NEUTRAL
    2, 3 -> com.solgrid.mobile.core.components.BadgeTone.ACCENT
    else -> com.solgrid.mobile.core.components.BadgeTone.NEUTRAL
}

/** The inline "tap a marker or result to peek its slots" panel shown in place of the results list.
 * Tapping an available slot goes straight to reservation creation for this node. */
@Composable
private fun SelectedNodePanel(
    loading: Boolean,
    node: com.solgrid.mobile.core.network.SolarStationDto?,
    slots: List<com.solgrid.mobile.core.network.BookingSlotDto>,
    onBack: () -> Unit,
    onOpenDetail: () -> Unit,
    onBookSlot: () -> Unit,
) {
    val colors = SolGridTheme.colors
    Column(Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier.fillMaxWidth().padding(horizontal = Spacing.lg, vertical = Spacing.sm),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(Spacing.sm),
        ) {
            Icon(
                Icons.AutoMirrored.Outlined.ArrowBack,
                contentDescription = "Back to results",
                tint = colors.textPrimary,
                modifier = Modifier.size(20.dp).clickableNoRipple(onBack),
            )
            Column(Modifier.weight(1f)) {
                Text(node?.name ?: "Loading…", style = AppType.sectionTitle, color = colors.textPrimary)
                if (node != null) {
                    Text(node.addressLine, style = AppType.supporting, color = colors.textSecondary)
                }
            }
        }

        if (loading) {
            Box(Modifier.fillMaxWidth().padding(Spacing.xxl), contentAlignment = Alignment.Center) {
                CircularProgressIndicator(color = colors.accent)
            }
            return@Column
        }

        if (node == null) {
            StatePlaceholder(
                com.solgrid.mobile.core.components.EmptyStateIcons.NoActivity,
                "Couldn't load this node",
                "Try again from the results list.",
            )
            return@Column
        }

        LazyColumn(
            modifier = Modifier.fillMaxWidth().padding(horizontal = Spacing.lg),
            contentPadding = PaddingValues(bottom = Spacing.sm),
        ) {
            if (slots.isEmpty()) {
                item {
                    Text(
                        "No battery slots configured for this node.",
                        style = AppType.body,
                        color = colors.textSecondary,
                        modifier = Modifier.padding(vertical = Spacing.md),
                    )
                }
            }
            items(slots, key = { it.id }) { slot ->
                val isBookable = slot.isActive && slot.isAvailable
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .then(if (isBookable) Modifier.clickableNoRipple(onBookSlot) else Modifier)
                        .padding(vertical = Spacing.md),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.SpaceBetween,
                ) {
                    Column {
                        Text("Slot ${slot.slotNumber} · ${slot.batteryCapacityKwh} kWh", style = AppType.bodyStrong, color = colors.textPrimary)
                        Text(formatSlotTimeRange(slot.startTime, slot.endTime), style = AppType.supporting, color = colors.textSecondary)
                    }
                    com.solgrid.mobile.core.components.StatusBadge(
                        text = slotStatusLabel(slot.status, slot.isAvailable),
                        tone = slotStatusTone(slot.status, slot.isAvailable),
                    )
                }
                com.solgrid.mobile.core.components.AppDivider()
            }
            item {
                androidx.compose.material3.TextButton(onClick = onOpenDetail, modifier = Modifier.padding(top = Spacing.sm)) {
                    Text("View full node details", style = AppType.bodyStrong, color = colors.accent)
                }
            }
        }
    }
}

/** A location-pin icon with no background tile, plus title/subtitle — used for search suggestions
 * and result rows here, where a plain pin reads better than `ListRow`'s boxed-icon style. */
@Composable
private fun BareIconRow(
    title: String,
    modifier: Modifier = Modifier,
    subtitle: String? = null,
    showChevron: Boolean = false,
    onClick: (() -> Unit)? = null,
) {
    val colors = SolGridTheme.colors
    Row(
        modifier = modifier
            .fillMaxWidth()
            .then(if (onClick != null) Modifier.clickableNoRipple(onClick) else Modifier)
            .padding(vertical = Spacing.md),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(Spacing.md),
    ) {
        Icon(Icons.Outlined.LocationOn, contentDescription = null, tint = colors.accent, modifier = Modifier.size(22.dp))
        Column(modifier = Modifier.weight(1f)) {
            Text(title, style = AppType.bodyStrong, color = colors.textPrimary, maxLines = 1, overflow = androidx.compose.ui.text.style.TextOverflow.Ellipsis)
            if (subtitle != null) {
                Text(subtitle, style = AppType.supporting, color = colors.textSecondary, maxLines = 1, overflow = androidx.compose.ui.text.style.TextOverflow.Ellipsis)
            }
        }
        if (showChevron) {
            Icon(Icons.Outlined.ChevronRight, contentDescription = null, tint = colors.textTertiary)
        }
    }
}

/**
 * Full-bleed map with a floating pill search bar (area suggestions appear inline, in a dropdown
 * under the bar) and a results sheet docked to the bottom — matches the "map behind everything,
 * search and results float over it" layout pattern instead of a fixed top bar + separate list.
 */
@Composable
fun NodesMapScreen(viewModel: NodeViewModel, onBack: () -> Unit, onNodeClick: (String) -> Unit, onBookSlot: (String) -> Unit) {
    val state by viewModel.state.collectAsState()
    val colors = SolGridTheme.colors
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    val locationHelper = remember { LocationHelper(context) }
    val placesClient = remember { if (Places.isInitialized()) Places.createClient(context) else null }

    var hasSearched by remember { mutableStateOf(false) }
    var searchText by remember { mutableStateOf("") }
    var suggestions by remember { mutableStateOf<List<AutocompletePrediction>>(emptyList()) }
    var sessionToken by remember { mutableStateOf(AutocompleteSessionToken.newInstance()) }
    var selectedNodeId by remember { mutableStateOf<String?>(null) }

    fun loadCurrentLocation() {
        viewModel.beginLocationSearch()
        scope.launch {
            val location = locationHelper.getCurrentLocation()
            if (location != null) {
                viewModel.loadNearby(location.first, location.second, areaLabel = "your current location")
            } else {
                // A GPS fix genuinely could not be resolved (fresh fix timed out and no last-known
                // location is cached) — surface that explicitly rather than silently substituting an
                // unrelated fallback point, which looked like a real "no nearby nodes" result.
                viewModel.markLocationUnavailable()
            }
        }
    }

    val locationPermissionLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { granted ->
        if (granted) {
            loadCurrentLocation()
        } else {
            viewModel.markLocationUnavailable()
        }
    }

    fun requestLocation() {
        hasSearched = true
        searchText = ""
        suggestions = emptyList()
        // A fresh search invalidates whatever node was peeked from the previous results — otherwise
        // the bottom sheet keeps showing the old node's slots instead of falling back to the new list.
        selectedNodeId = null
        viewModel.clearPeekedNode()
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

    fun selectPrediction(prediction: AutocompletePrediction) {
        val client = placesClient ?: return
        val request = FetchPlaceRequest.newInstance(
            prediction.placeId,
            listOf(Place.Field.LAT_LNG, Place.Field.NAME, Place.Field.ADDRESS),
        )
        scope.launch {
            runCatching { client.fetchPlace(request).await() }.getOrNull()?.place?.let { place ->
                val latLng = place.latLng
                if (latLng != null) {
                    hasSearched = true
                    searchText = place.name ?: prediction.getPrimaryText(null).toString()
                    suggestions = emptyList()
                    selectedNodeId = null
                    viewModel.clearPeekedNode()
                    viewModel.loadNearby(latLng.latitude, latLng.longitude, areaLabel = place.name ?: place.address)
                }
            }
            sessionToken = AutocompleteSessionToken.newInstance()
        }
    }

    fun onSearchTextChange(text: String) {
        searchText = text
        val client = placesClient
        if (client == null || text.isBlank()) {
            suggestions = emptyList()
            return
        }
        val request = FindAutocompletePredictionsRequest.builder()
            .setQuery(text)
            .setSessionToken(sessionToken)
            .build()
        scope.launch {
            suggestions = runCatching { client.findAutocompletePredictions(request).await() }
                .getOrNull()?.autocompletePredictions ?: emptyList()
        }
    }

    LaunchedEffect(Unit) { requestLocation() }

    val hasLocation = state.searchLatitude != null && state.searchLongitude != null
    val camera = rememberCameraPositionState { position = CameraPosition.fromLatLngZoom(LatLng(6.9271, 79.8612), 6f) }
    var mapLoaded by remember { mutableStateOf(false) }

    // Auto-zoom to fit the search origin plus every result marker, rather than a fixed zoom level
    // — a tight cluster of nearby nodes zooms in close, while a wide 200km fallback search zooms
    // out far enough to show them all at once. Waits for `mapLoaded` because `newLatLngBounds`
    // needs the map's real on-screen size to compute a correct zoom; calling it before the map has
    // been laid out (e.g. on first composition) silently falls back to an over-zoomed single point.
    LaunchedEffect(state.searchLatitude, state.searchLongitude, state.nodes, mapLoaded) {
        val latitude = state.searchLatitude
        val longitude = state.searchLongitude
        if (latitude == null || longitude == null || !mapLoaded) return@LaunchedEffect

        val origin = LatLng(latitude, longitude)

        if (state.nodes.isEmpty()) {
            camera.position = CameraPosition.fromLatLngZoom(origin, 12f)
            return@LaunchedEffect
        }

        val boundsBuilder = LatLngBounds.builder().include(origin)
        state.nodes.forEach { node -> boundsBuilder.include(LatLng(node.location.latitude, node.location.longitude)) }
        runCatching {
            camera.animate(CameraUpdateFactory.newLatLngBounds(boundsBuilder.build(), 140))
        }.onFailure {
            camera.position = CameraPosition.fromLatLngZoom(origin, 12f)
        }

        // A single node (or several right on top of each other) produces a near-zero-area bounds
        // box, which `newLatLngBounds` zooms into all the way to street level. Cap the zoom back
        // down to a sensible neighborhood-scale level in that case.
        if (camera.position.zoom > MAX_AUTO_ZOOM) {
            camera.position = CameraPosition.fromLatLngZoom(camera.position.target, MAX_AUTO_ZOOM)
        }
    }

    Box(Modifier.fillMaxSize().background(colors.background)) {
        if (hasLocation) {
            GoogleMap(
                modifier = Modifier.fillMaxSize(),
                cameraPositionState = camera,
                uiSettings = MapUiSettings(zoomControlsEnabled = false, myLocationButtonEnabled = false),
                onMapLoaded = { mapLoaded = true },
            ) {
                state.nodes.forEach { node ->
                    Marker(
                        state = MarkerState(LatLng(node.location.latitude, node.location.longitude)),
                        title = node.name,
                        snippet = "${node.availableSlotCount} slots available",
                        icon = BitmapDescriptorFactory.defaultMarker(BitmapDescriptorFactory.HUE_GREEN),
                        onClick = {
                            selectedNodeId = node.id
                            viewModel.peekNode(node.id)
                            true
                        },
                    )
                }
            }
        } else {
            Box(Modifier.fillMaxSize().background(colors.surfaceAlt))
        }

        // Floating top bar: back button + pill search field, overlaid directly on the map.
        Column(Modifier.fillMaxWidth().statusBarsPadding().padding(Spacing.lg)) {
            Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(Spacing.sm)) {
                Box(
                    modifier = Modifier
                        .size(44.dp)
                        .shadow(4.dp, CircleShape)
                        .clip(CircleShape)
                        .background(colors.surface)
                        .clickableNoRipple { onBack() },
                    contentAlignment = Alignment.Center,
                ) {
                    Icon(Icons.AutoMirrored.Outlined.ArrowBack, contentDescription = "Back", tint = colors.textPrimary)
                }

                Row(
                    modifier = Modifier
                        .weight(1f)
                        .shadow(4.dp, RoundedCornerShape(Radius.pill))
                        .clip(RoundedCornerShape(Radius.pill))
                        .background(colors.surface)
                        .padding(horizontal = Spacing.md, vertical = Spacing.sm),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(Spacing.sm),
                ) {
                    Icon(Icons.Outlined.Search, contentDescription = null, tint = colors.textSecondary, modifier = Modifier.size(20.dp))
                    androidx.compose.foundation.text.BasicTextField(
                        value = searchText,
                        onValueChange = { onSearchTextChange(it) },
                        modifier = Modifier.weight(1f),
                        singleLine = true,
                        textStyle = AppType.body.copy(color = colors.textPrimary),
                        cursorBrush = androidx.compose.ui.graphics.SolidColor(colors.accent),
                        decorationBox = { inner ->
                            if (searchText.isEmpty()) {
                                Text("Enter an area, see nearby grids", style = AppType.body, color = colors.textTertiary)
                            }
                            inner()
                        },
                    )
                    if (searchText.isNotEmpty()) {
                        Icon(
                            Icons.Filled.Close,
                            contentDescription = "Clear search",
                            tint = colors.textTertiary,
                            modifier = Modifier.size(18.dp).clickableNoRipple {
                                searchText = ""
                                suggestions = emptyList()
                            },
                        )
                    }
                }

                Box(
                    modifier = Modifier
                        .size(44.dp)
                        .shadow(4.dp, CircleShape)
                        .clip(CircleShape)
                        .background(colors.accent)
                        .clickableNoRipple { requestLocation() },
                    contentAlignment = Alignment.Center,
                ) {
                    Icon(Icons.Outlined.MyLocation, contentDescription = "Use my location", tint = colors.surface)
                }
            }

            // Inline suggestions dropdown — replaces the old full-screen Places Autocomplete intent.
            if (suggestions.isNotEmpty()) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(top = Spacing.sm)
                        .shadow(6.dp, RoundedCornerShape(Radius.lg))
                        .clip(RoundedCornerShape(Radius.lg))
                        .background(colors.surface),
                ) {
                    suggestions.forEach { prediction ->
                        BareIconRow(
                            title = prediction.getPrimaryText(null).toString(),
                            subtitle = prediction.getSecondaryText(null).toString().ifBlank { null },
                            modifier = Modifier.padding(horizontal = Spacing.md),
                            onClick = { selectPrediction(prediction) },
                        )
                    }
                }
            }
        }

        // Results sheet, docked to the bottom over the map.
        Column(
            modifier = Modifier
                .align(Alignment.BottomCenter)
                .fillMaxWidth()
                .shadow(12.dp, RoundedCornerShape(topStart = Radius.xl, topEnd = Radius.xl))
                .clip(RoundedCornerShape(topStart = Radius.xl, topEnd = Radius.xl))
                .background(colors.surface)
                .padding(bottom = Spacing.lg),
        ) {
            Box(Modifier.fillMaxWidth().padding(top = Spacing.sm), contentAlignment = Alignment.Center) {
                Box(Modifier.size(width = 36.dp, height = 4.dp).clip(RoundedCornerShape(Radius.pill)).background(colors.border))
            }

            when {
                state.loading -> Box(Modifier.fillMaxWidth().padding(Spacing.xxl), contentAlignment = Alignment.Center) {
                    CircularProgressIndicator(color = colors.accent)
                }
                state.locationUnavailable -> StatePlaceholder(
                    Icons.Outlined.MyLocation,
                    "Couldn't find your location",
                    "We couldn't get a GPS fix on this device. Check location is enabled, or search an area above.",
                    actionText = "Try again",
                    onAction = { requestLocation() },
                )
                state.error != null -> ErrorState(ErrorKind.SERVER_ERROR, onRetry = { requestLocation() })
                state.nodes.isEmpty() -> StatePlaceholder(
                    com.solgrid.mobile.core.components.EmptyStateIcons.NoActivity,
                    "No nearby nodes",
                    "Try searching a different area above.",
                )
                selectedNodeId != null -> SelectedNodePanel(
                    loading = state.peekLoading,
                    node = state.peekedNode,
                    slots = state.peekedSlots,
                    onBack = {
                        selectedNodeId = null
                        viewModel.clearPeekedNode()
                    },
                    onOpenDetail = { onNodeClick(selectedNodeId!!) },
                    onBookSlot = { onBookSlot(selectedNodeId!!) },
                )
                else -> {
                    Row(Modifier.fillMaxWidth().padding(horizontal = Spacing.lg, vertical = Spacing.sm)) {
                        Text(
                            state.searchAreaLabel?.let { "${state.nodes.size} nodes near $it" } ?: "${state.nodes.size} nodes",
                            style = AppType.sectionTitle,
                            color = colors.textPrimary,
                        )
                    }
                    LazyColumn(
                        modifier = Modifier.fillMaxWidth().padding(horizontal = Spacing.lg),
                        contentPadding = PaddingValues(bottom = Spacing.sm),
                    ) {
                        items(state.nodes, key = { it.id }) { node ->
                            BareIconRow(
                                title = node.name,
                                subtitle = "${node.addressLine} · ${node.availableSlotCount}/${node.totalSlotCount} slots",
                                showChevron = true,
                                onClick = {
                                    selectedNodeId = node.id
                                    viewModel.peekNode(node.id)
                                },
                            )
                        }
                    }
                }
            }
        }
    }
}
