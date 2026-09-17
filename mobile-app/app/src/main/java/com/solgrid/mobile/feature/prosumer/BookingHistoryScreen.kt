package com.solgrid.mobile.feature.prosumer

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.EmptyKind
import com.solgrid.mobile.core.components.EmptyState
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing

/** Past (completed/cancelled) bookings (MOB-06). */
@Composable
fun BookingHistoryScreen(viewModel: ProsumerViewModel, onBack: () -> Unit, onBookingClick: (String) -> Unit) {
    val colors = SolGridTheme.colors
    val state by viewModel.uiState.collectAsState()

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Booking History", onBack = onBack)
        androidx.compose.runtime.LaunchedEffect(Unit) { viewModel.loadReservations() }
        state.reservationsError?.let { androidx.compose.material3.Text(it, color = colors.error); com.solgrid.mobile.core.components.SecondaryButton("Retry", { viewModel.loadReservations() }) }
        if (state.reservationsLoading) androidx.compose.material3.Text("Loading history…")
        if (state.history.isEmpty() && !state.reservationsLoading && state.reservationsError == null) {
            EmptyState(kind = EmptyKind.ACTIVITY, modifier = Modifier.padding(top = Spacing.xxxl))
        } else if (state.history.isNotEmpty()) {
            LazyColumn(contentPadding = PaddingValues(horizontal = Spacing.lg)) {
                items(state.history, key = { it.id }) { reservation ->
                    ReservationRow(reservation = reservation, onClick = { onBookingClick(reservation.id) })
                }
            }
        }
    }
}
