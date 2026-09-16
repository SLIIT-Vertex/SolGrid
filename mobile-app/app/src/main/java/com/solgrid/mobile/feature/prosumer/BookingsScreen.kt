package com.solgrid.mobile.feature.prosumer

import androidx.compose.foundation.background
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.EmptyKind
import com.solgrid.mobile.core.components.EmptyState
import com.solgrid.mobile.core.components.FilterChip
import com.solgrid.mobile.core.components.SearchField
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.models.ReservationStatus

private enum class BookingFilter(val label: String) { ALL("All"), PENDING("Pending"), APPROVED("Approved") }

/** Current/pending bookings with search & filter (MOB-06). Data comes live from the ViewModel. */
@Composable
fun BookingsScreen(viewModel: ProsumerViewModel, onBack: () -> Unit, onBookingClick: (String) -> Unit) {
    val colors = SolGridTheme.colors
    val state by viewModel.uiState.collectAsState()
    var query by remember { mutableStateOf("") }
    var filter by remember { mutableStateOf(BookingFilter.ALL) }

    val filtered = state.currentAndPending
        .filter { it.nodeName.contains(query, ignoreCase = true) || query.isBlank() }
        .filter {
            when (filter) {
                BookingFilter.ALL -> true
                BookingFilter.PENDING -> it.status == ReservationStatus.PENDING
                BookingFilter.APPROVED -> it.status == ReservationStatus.APPROVED
            }
        }

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "My Bookings", onBack = onBack)
        Column(modifier = Modifier.fillMaxWidth().padding(horizontal = Spacing.lg, vertical = Spacing.sm)) {
            SearchField(value = query, onValueChange = { query = it }, placeholder = "Search by node name...")
            Row(
                modifier = Modifier.fillMaxWidth().padding(top = Spacing.sm).horizontalScroll(rememberScrollState()),
                horizontalArrangement = Arrangement.spacedBy(Spacing.sm)
            ) {
                BookingFilter.entries.forEach { f ->
                    FilterChip(label = f.label, selected = filter == f, onClick = { filter = f })
                }
            }
        }

        if (filtered.isEmpty()) {
            EmptyState(kind = EmptyKind.SEARCH_RESULTS, modifier = Modifier.padding(top = Spacing.xxxl))
        } else {
            LazyColumn(contentPadding = PaddingValues(horizontal = Spacing.lg)) {
                items(filtered, key = { it.id }) { reservation ->
                    ReservationRow(reservation = reservation, onClick = { onBookingClick(reservation.id) })
                }
            }
        }
    }
}
