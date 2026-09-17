package com.solgrid.mobile.feature.reservations
import com.solgrid.mobile.feature.prosumer.ProsumerViewModel

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.CalendarMonth
import androidx.compose.material.icons.outlined.Cancel
import androidx.compose.material.icons.outlined.CheckCircle
import androidx.compose.material.icons.outlined.ConfirmationNumber
import androidx.compose.material.icons.outlined.Edit
import androidx.compose.material.icons.outlined.EvStation
import androidx.compose.material.icons.outlined.HourglassEmpty
import androidx.compose.material.icons.outlined.QrCode2
import androidx.compose.material.icons.outlined.Schedule
import androidx.compose.material.icons.outlined.Tag
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
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.BadgeTone
import com.solgrid.mobile.core.components.ConfirmationDialog
import com.solgrid.mobile.core.components.EmptyStateIcons
import com.solgrid.mobile.core.components.QrCodeVisual
import com.solgrid.mobile.core.components.SecondaryButton
import com.solgrid.mobile.core.components.StatePlaceholder
import com.solgrid.mobile.core.components.StatusBadge
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.Sizing
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.models.ReservationStatus
import com.solgrid.mobile.feature.microgrid.NodeViewModel
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter

/**
 * Read-only booking details (MOB-06/09). Editing lives on a separate Reschedule screen reached
 * via the pencil in the top bar; the secure transaction QR is revealed inline for approved
 * bookings. All rules (12-hour notice) stay server-authoritative.
 */
@Composable
fun BookingDetailScreen(
    viewModel: ProsumerViewModel,
    reservationId: String,
    onBack: () -> Unit,
    onEdit: () -> Unit,
    onCancelled: () -> Unit
) {
    val colors = SolGridTheme.colors
    val state by viewModel.uiState.collectAsStateWithLifecycle()
    val reservation = state.reservations.find { it.id == reservationId }

    val nodeViewModel = remember { NodeViewModel() }
    val nodeState by nodeViewModel.state.collectAsState()
    LaunchedEffect(reservation?.nodeId) { reservation?.nodeId?.let { nodeViewModel.loadDetail(it) } }
    LaunchedEffect(Unit) { viewModel.loadReservations() }

    val slot = remember(nodeState.slots, reservation?.bookingSlotId) {
        nodeState.slots.find { it.id == reservation?.bookingSlotId }
    }

    var showCancelConfirm by remember { mutableStateOf(false) }
    var actionError by remember { mutableStateOf<String?>(null) }

    val canModify = reservation?.canModify == true
    val windowError = reservation?.let { viewModel.validateModifyWindow(it) }
    val canEdit = canModify && windowError == null

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(
            title = "Booking Details",
            onBack = onBack,
            actions = {
                if (canEdit) {
                    IconButton(onClick = onEdit) {
                        Box(
                            modifier = Modifier.size(36.dp).clip(CircleShape).background(colors.surface),
                            contentAlignment = Alignment.Center
                        ) {
                            Icon(Icons.Outlined.Edit, contentDescription = "Reschedule", tint = colors.textPrimary, modifier = Modifier.size(18.dp))
                        }
                    }
                }
            }
        )

        if (reservation == null) {
            StatePlaceholder(
                icon = EmptyStateIcons.NoActivity,
                title = "Booking not found",
                description = "This reservation may have been removed. Pull back and refresh your bookings.",
                modifier = Modifier.padding(top = Spacing.xxxl)
            )
            return@Column
        }

        Column(
            modifier = Modifier
                .weight(1f)
                .verticalScroll(rememberScrollState())
                .padding(horizontal = Spacing.lg),
            verticalArrangement = Arrangement.spacedBy(Spacing.lg)
        ) {
            StatusHero(status = reservation.status, modifier = Modifier.padding(top = Spacing.md))

            // Approved bookings surface the transaction QR first — it's the thing the prosumer needs at the station.
            if (reservation.status == ReservationStatus.APPROVED) {
                QrPanel(viewModel = viewModel, reservationId = reservationId)
            }

            DetailCard(title = "Reservation") {
                DetailRow(label = "Station", value = reservation.nodeName, icon = Icons.Outlined.EvStation)
                ThinDivider()
                DetailRow(label = "Date", value = reservation.date, icon = Icons.Outlined.CalendarMonth)
                ThinDivider()
                DetailRow(label = "Time", value = "${reservation.startTime} – ${reservation.endTime}", icon = Icons.Outlined.Schedule)
                if (slot != null) {
                    ThinDivider()
                    DetailRow(label = "Battery slot", value = "Slot ${slot.slotNumber} · ${slot.batteryCapacityKwh.toInt()} kWh", icon = Icons.Outlined.Tag)
                }
            }

            DetailCard(title = "Reference") {
                DetailRow(label = "Booking reference", value = reservation.reference, icon = Icons.Outlined.ConfirmationNumber)
                ThinDivider()
                DetailRow(label = "Requested on", value = reservation.createdAt)
            }

            if (reservation.status == ReservationStatus.REJECTED && !reservation.rejectionReason.isNullOrBlank()) {
                NoteCard(
                    text = "Reason: ${reservation.rejectionReason}",
                    tone = colors.error,
                    surface = colors.errorSurface
                )
            }

            if (reservation.status == ReservationStatus.PENDING) {
                NoteCard(
                    text = "Your transaction QR unlocks here as soon as a Grid Operator approves this request.",
                    tone = colors.warning,
                    surface = colors.warningSurface
                )
            }

            if (windowError != null && (reservation.status == ReservationStatus.PENDING || reservation.status == ReservationStatus.APPROVED)) {
                Text(windowError, style = AppType.caption, color = colors.error)
            }
            actionError?.let { Text(it, style = AppType.caption, color = colors.error) }

            if (canModify) {
                Text(
                    "You can reschedule or cancel this booking up to 12 hours before its start time.",
                    style = AppType.caption,
                    color = colors.textTertiary
                )
            }
            Box(modifier = Modifier.padding(bottom = Spacing.sm))
        }

        if (canModify) {
            SecondaryButton(
                text = "Cancel Reservation",
                enabled = windowError == null,
                onClick = { showCancelConfirm = true },
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = Spacing.lg, vertical = Spacing.md)
            )
        }
    }

    if (showCancelConfirm && reservation != null) {
        ConfirmationDialog(
            title = "Cancel this reservation?",
            message = "This frees the slot for other prosumers and can't be undone.",
            confirmText = "Cancel Reservation",
            dismissText = "Keep Booking",
            destructive = true,
            onConfirm = {
                showCancelConfirm = false
                viewModel.cancelReservation(
                    reservation.id,
                    onError = { actionError = it },
                    onDone = onCancelled
                )
            },
            onDismiss = { showCancelConfirm = false }
        )
    }
}

@Composable
private fun StatusHero(status: ReservationStatus, modifier: Modifier = Modifier) {
    val colors = SolGridTheme.colors
    data class Hero(val icon: ImageVector, val tint: Color, val surface: Color, val title: String, val message: String)
    val hero = when (status) {
        ReservationStatus.PENDING -> Hero(Icons.Outlined.HourglassEmpty, colors.warning, colors.warningSurface, "Pending approval", "Waiting for a Grid Operator to review your request.")
        ReservationStatus.APPROVED -> Hero(Icons.Outlined.CheckCircle, colors.success, colors.successSurface, "Approved", "You're all set. Show your QR code at the station.")
        ReservationStatus.REJECTED -> Hero(Icons.Outlined.Cancel, colors.error, colors.errorSurface, "Declined", "This request wasn't approved. You can book another slot.")
        ReservationStatus.CANCELLED -> Hero(Icons.Outlined.Cancel, colors.textSecondary, colors.surfaceAlt, "Cancelled", "This booking was cancelled and the slot released.")
        ReservationStatus.COMPLETED -> Hero(Icons.Outlined.CheckCircle, colors.success, colors.successSurface, "Completed", "Energy transfer finished. Thanks for trading on SolGrid.")
    }
    Row(
        modifier = modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(Radius.lg))
            .background(hero.surface)
            .padding(Spacing.lg),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(Spacing.md)
    ) {
        Box(
            modifier = Modifier.size(48.dp).clip(CircleShape).background(colors.background.copy(alpha = 0.6f)),
            contentAlignment = Alignment.Center
        ) {
            Icon(hero.icon, contentDescription = null, tint = hero.tint, modifier = Modifier.size(26.dp))
        }
        Column(modifier = Modifier.weight(1f)) {
            Text(hero.title, style = AppType.sectionTitle, color = colors.textPrimary)
            Text(hero.message, style = AppType.supporting, color = colors.textSecondary, modifier = Modifier.padding(top = Spacing.xxs))
        }
    }
}

/** Inline secure transaction QR — issued fresh from the server and shown by default for approved
 * bookings. The encoded token is short-lived (server-enforced), so it can be refreshed on expiry. */
@Composable
private fun QrPanel(viewModel: ProsumerViewModel, reservationId: String) {
    val colors = SolGridTheme.colors
    var payload by remember(reservationId) { mutableStateOf<String?>(null) }
    var expiresAt by remember(reservationId) { mutableStateOf<String?>(null) }
    var loading by remember(reservationId) { mutableStateOf(true) }
    var error by remember(reservationId) { mutableStateOf<String?>(null) }
    var expired by remember(reservationId) { mutableStateOf(false) }

    fun requestQr() {
        loading = true; error = null; payload = null; expiresAt = null; expired = false
        viewModel.issueReservationQr(reservationId, onError = { loading = false; error = it }) { value, expiry ->
            loading = false; payload = value; expiresAt = expiry
        }
    }
    // Reuse the previously issued QR (from SQLite) if it's still valid; only mint a new one when
    // there isn't a usable cached code. The user can always force a fresh one with "Regenerate".
    LaunchedEffect(reservationId) {
        val cached = viewModel.cachedQr(reservationId)
        if (cached != null) { payload = cached.first; expiresAt = cached.second; loading = false } else requestQr()
    }
    LaunchedEffect(expiresAt) {
        val expiry = expiresAt ?: return@LaunchedEffect
        val ms = java.time.Duration.between(java.time.Instant.now(), OffsetDateTime.parse(expiry).toInstant()).toMillis()
        if (ms > 0) kotlinx.coroutines.delay(ms)
        expired = true
    }

    Column(
        modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(Radius.lg))
            .background(colors.surface)
            .border(Sizing.borderThin, colors.border, RoundedCornerShape(Radius.lg))
            .padding(Spacing.lg),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Row(modifier = Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(Spacing.md)) {
            Box(
                modifier = Modifier.size(40.dp).clip(RoundedCornerShape(Radius.md)).background(colors.accentSurface),
                contentAlignment = Alignment.Center
            ) {
                Icon(Icons.Outlined.QrCode2, contentDescription = null, tint = colors.accent)
            }
            Column(modifier = Modifier.weight(1f)) {
                Text("Transaction QR", style = AppType.bodyStrong, color = colors.textPrimary)
                Text("Show this to the Grid Operator to start the transfer.", style = AppType.caption, color = colors.textSecondary)
            }
            StatusBadge(text = "Approved", tone = BadgeTone.SUCCESS)
        }

        Column(modifier = Modifier.fillMaxWidth().padding(top = Spacing.lg), horizontalAlignment = Alignment.CenterHorizontally) {
            when {
                loading -> Box(modifier = Modifier.size(220.dp), contentAlignment = Alignment.Center) { CircularProgressIndicator(color = colors.accent) }
                error != null -> {
                    Text(error!!, style = AppType.body, color = colors.error, textAlign = TextAlign.Center)
                    SecondaryButton("Try again", { requestQr() }, modifier = Modifier.padding(top = Spacing.md))
                }
                expired -> {
                    Text("For your security this code expired. Generate a fresh one to present at the station.", style = AppType.body, color = colors.textSecondary, textAlign = TextAlign.Center)
                    SecondaryButton("Regenerate QR", { requestQr() }, modifier = Modifier.padding(top = Spacing.md))
                }
                payload != null -> {
                    QrCodeVisual(payload = payload!!)
                    expiresAt?.let {
                        val time = OffsetDateTime.parse(it).atZoneSameInstant(ZoneId.systemDefault()).format(DateTimeFormatter.ofPattern("MMM d, HH:mm"))
                        Text("Valid until $time", style = AppType.caption, color = colors.textTertiary, modifier = Modifier.padding(top = Spacing.md))
                    }
                    SecondaryButton("Regenerate QR", { requestQr() }, modifier = Modifier.padding(top = Spacing.md))
                }
            }
        }
    }
}

@Composable
private fun NoteCard(text: String, tone: Color, surface: Color) {
    Box(
        modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(Radius.lg))
            .background(surface)
            .padding(Spacing.lg)
    ) {
        Text(text, style = AppType.supporting, color = tone)
    }
}

@Composable
private fun ThinDivider() {
    val colors = SolGridTheme.colors
    Box(
        modifier = Modifier
            .fillMaxWidth()
            .padding(start = Sizing.iconMd + Spacing.md)
            .height(1.dp)
            .background(colors.border)
    )
}
