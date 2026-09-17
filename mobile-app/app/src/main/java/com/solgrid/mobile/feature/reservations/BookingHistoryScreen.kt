package com.solgrid.mobile.feature.reservations
import com.solgrid.mobile.feature.prosumer.ProsumerViewModel

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import com.solgrid.mobile.core.components.AppPullToRefresh
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.EmptyKind
import com.solgrid.mobile.core.components.EmptyState
import com.solgrid.mobile.core.components.ErrorKind
import com.solgrid.mobile.core.components.ErrorState
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing

/** Past (completed/cancelled/rejected) bookings (MOB-06). */
@Composable
fun BookingHistoryScreen(viewModel: ProsumerViewModel, onBack: () -> Unit, onBookingClick: (String) -> Unit) {
    val colors = SolGridTheme.colors
    val state by viewModel.uiState.collectAsState()

    LaunchedEffect(Unit) { viewModel.loadReservations() }

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Booking History", onBack = onBack)
        AppPullToRefresh(
            refreshing = state.reservationsLoading && state.reservations.isNotEmpty(),
            onRefresh = { viewModel.loadReservations() },
            modifier = Modifier.fillMaxSize()
        ) {
            when {
                state.reservationsLoading && state.reservations.isEmpty() ->
                    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        CircularProgressIndicator(color = colors.accent)
                    }

                state.reservationsError != null && state.reservations.isEmpty() ->
                    ErrorState(kind = ErrorKind.SERVER_ERROR, onRetry = { viewModel.loadReservations() }, modifier = Modifier.padding(top = Spacing.xxxl))

                state.history.isEmpty() ->
                    EmptyState(kind = EmptyKind.ACTIVITY, modifier = Modifier.padding(top = Spacing.xxxl))

                else -> LazyColumn(
                    modifier = Modifier.fillMaxSize(),
                    contentPadding = PaddingValues(horizontal = Spacing.lg, vertical = Spacing.sm),
                    verticalArrangement = Arrangement.spacedBy(Spacing.md)
                ) {
                    items(state.history, key = { it.id }) { reservation ->
                        BookingListCard(reservation = reservation, onClick = { onBookingClick(reservation.id) })
                    }
                }
            }
        }
    }
}
