package com.solgrid.mobile.feature.operator

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Badge
import androidx.compose.material.icons.outlined.CalendarMonth
import androidx.compose.material.icons.outlined.ConfirmationNumber
import androidx.compose.material.icons.outlined.EvStation
import androidx.compose.material.icons.outlined.Person
import androidx.compose.material.icons.outlined.Place
import androidx.compose.material.icons.outlined.Schedule
import androidx.compose.material.icons.outlined.Tag
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.StatusBadge
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.feature.microgrid.NodeViewModel
import com.solgrid.mobile.feature.reservations.DetailCard
import com.solgrid.mobile.feature.reservations.DetailRow
import com.solgrid.mobile.feature.reservations.statusLabel
import com.solgrid.mobile.feature.reservations.statusTone

/** Read-only booking detail for a Grid Operator opened from the queue — shows the full prosumer,
 * booking, and station/slot context so the operator can review without scanning a QR. */
@Composable
fun OperatorBookingDetailScreen(
    viewModel: OperatorViewModel,
    reservationId: String,
    onBack: () -> Unit,
) {
    val colors = SolGridTheme.colors
    val state by viewModel.uiState.collectAsState()
    val reservation = state.selected?.takeIf { it.id == reservationId }

    val nodeViewModel = remember { NodeViewModel() }
    val nodeState by nodeViewModel.state.collectAsState()

    LaunchedEffect(reservationId) { viewModel.loadReservationDetail(reservationId) }
    LaunchedEffect(reservation?.nodeId) { reservation?.nodeId?.let { nodeViewModel.loadDetail(it) } }

    val node = nodeState.selected
    val slot = remember(nodeState.slots, reservation?.bookingSlotId) {
        nodeState.slots.find { it.id == reservation?.bookingSlotId }
    }

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Booking Details", onBack = onBack)

        when {
            state.selectedLoading && reservation == null ->
                Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    CircularProgressIndicator(color = colors.accent)
                }

            reservation == null ->
                Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    Text(
                        state.selectedError ?: "Booking not found.",
                        style = AppType.body,
                        color = colors.textSecondary,
                        modifier = Modifier.padding(horizontal = Spacing.xxl)
                    )
                }

            else -> Column(
                modifier = Modifier
                    .weight(1f)
                    .verticalScroll(rememberScrollState())
                    .padding(horizontal = Spacing.lg),
                verticalArrangement = Arrangement.spacedBy(Spacing.lg)
            ) {
                Box(modifier = Modifier.padding(top = Spacing.md))
                StatusBadge(text = statusLabel(reservation.status), tone = statusTone(reservation.status))

                DetailCard(title = "Prosumer") {
                    reservation.prosumerName?.takeIf { it.isNotBlank() }?.let {
                        DetailRow(label = "Name", value = it, icon = Icons.Outlined.Person)
                    }
                    DetailRow(label = "NIC", value = reservation.prosumerNic, icon = Icons.Outlined.Badge)
                }

                DetailCard(title = "Booking") {
                    DetailRow(label = "Reference", value = reservation.reference, icon = Icons.Outlined.ConfirmationNumber)
                    DetailRow(label = "Date", value = reservation.date, icon = Icons.Outlined.CalendarMonth)
                    DetailRow(label = "Time", value = "${reservation.startTime} – ${reservation.endTime}", icon = Icons.Outlined.Schedule)
                }

                DetailCard(title = "Station & slot") {
                    DetailRow(label = "Station", value = node?.name ?: reservation.nodeName, icon = Icons.Outlined.EvStation)
                    node?.addressLine?.takeIf { it.isNotBlank() }?.let {
                        DetailRow(label = "Address", value = it, icon = Icons.Outlined.Place)
                    }
                    node?.let { DetailRow(label = "Capacity", value = "${it.capacityKw} kW") }
                    slot?.let {
                        DetailRow(label = "Battery slot", value = "Slot ${it.slotNumber} · ${it.batteryCapacityKwh.toInt()} kWh", icon = Icons.Outlined.Tag)
                    }
                }

                Box(modifier = Modifier.padding(bottom = Spacing.lg))
            }
        }
    }
}
