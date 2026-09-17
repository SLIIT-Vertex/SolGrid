package com.solgrid.mobile.feature.reservations
import com.solgrid.mobile.feature.prosumer.ProsumerViewModel

import androidx.compose.foundation.background
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.History
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
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.EmptyKind
import com.solgrid.mobile.core.components.EmptyState
import com.solgrid.mobile.core.components.ErrorKind
import com.solgrid.mobile.core.components.ErrorState
import com.solgrid.mobile.core.components.FilterChip
import com.solgrid.mobile.core.components.SearchField
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.models.ReservationStatus

private enum class BookingFilter(val label: String) { ALL("All"), PENDING("Pending"), APPROVED("Approved") }

/** Current/pending bookings with search & filter (MOB-06). Data comes live from the ViewModel. */
@Composable
fun BookingsScreen(
    viewModel: ProsumerViewModel,
    onBack: () -> Unit,
    onBookingClick: (String) -> Unit,
    onViewHistory: () -> Unit
) {
    val colors = SolGridTheme.colors
    val state by viewModel.uiState.collectAsState()
    var query by remember { mutableStateOf("") }
    var filter by remember { mutableStateOf(BookingFilter.ALL) }

    LaunchedEffect(Unit) { viewModel.loadReservations() }

    val filtered = state.currentAndPending
        .filter { query.isBlank() || it.nodeName.contains(query, ignoreCase = true) }
        .filter {
            when (filter) {
                BookingFilter.ALL -> true
                BookingFilter.PENDING -> it.status == ReservationStatus.PENDING
                BookingFilter.APPROVED -> it.status == ReservationStatus.APPROVED
            }
        }

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(
            title = "My Bookings",
            onBack = onBack,
            actions = {
                IconButton(onClick = onViewHistory) {
                    Icon(Icons.Outlined.History, contentDescription = "Booking history", tint = colors.textPrimary)
                }
            }
        )
        Column(modifier = Modifier.fillMaxWidth().padding(horizontal = Spacing.lg, vertical = Spacing.sm)) {
            SearchField(value = query, onValueChange = { query = it }, placeholder = "Search by station name…")
            Row(
                modifier = Modifier.fillMaxWidth().padding(top = Spacing.sm).horizontalScroll(rememberScrollState()),
                horizontalArrangement = Arrangement.spacedBy(Spacing.sm)
            ) {
                BookingFilter.entries.forEach { f ->
                    FilterChip(label = f.label, selected = filter == f, onClick = { filter = f })
                }
            }
        }

        when {
            state.reservationsLoading && state.reservations.isEmpty() ->
                Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    CircularProgressIndicator(color = colors.accent)
                }

            state.reservationsError != null && state.reservations.isEmpty() ->
                ErrorState(kind = ErrorKind.SERVER_ERROR, onRetry = { viewModel.loadReservations() }, modifier = Modifier.padding(top = Spacing.xxxl))

            filtered.isEmpty() ->
                EmptyState(kind = EmptyKind.SEARCH_RESULTS, modifier = Modifier.padding(top = Spacing.xxxl))

            else -> {
                Text(
                    "${filtered.size} active ${if (filtered.size == 1) "booking" else "bookings"}",
                    style = AppType.caption,
                    color = colors.textTertiary,
                    modifier = Modifier.padding(horizontal = Spacing.lg, vertical = Spacing.xs)
                )
                LazyColumn(
                    contentPadding = PaddingValues(horizontal = Spacing.lg, vertical = Spacing.sm),
                    verticalArrangement = Arrangement.spacedBy(Spacing.md)
                ) {
                    items(filtered, key = { it.id }) { reservation ->
                        BookingListCard(reservation = reservation, onClick = { onBookingClick(reservation.id) })
                    }
                }
            }
        }
    }
}
