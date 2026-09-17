package com.solgrid.mobile.feature.reservations
import com.solgrid.mobile.feature.prosumer.ProsumerViewModel

import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Cancel
import androidx.compose.material.icons.outlined.CheckCircle
import androidx.compose.material.icons.outlined.EditCalendar
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.solgrid.mobile.core.models.ReservationStatus
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.BadgeTone
import com.solgrid.mobile.core.components.StatusBadge
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.components.PrimaryButton
import com.solgrid.mobile.core.components.SecondaryButton
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing

enum class SummaryAction(val title: String, val description: String) {
    CREATED("Reservation Requested", "Your booking is pending approval. Your transaction QR will be available here once a Grid Operator approves it."),
    UPDATED("Reservation Updated", "Your changes have been saved and sent for confirmation."),
    CANCELLED("Reservation Cancelled", "This booking has been cancelled and the slot released.")
}

/** Shown after create/update/cancel actions (MOB-10). */
@Composable
fun ReservationSummaryScreen(
    action: SummaryAction,
    viewModel: ProsumerViewModel,
    reservationId: String,
    onViewBookings: () -> Unit,
    onBackToDashboard: () -> Unit
) {
    val state by viewModel.uiState.collectAsStateWithLifecycle()
    val reservation = state.reservations.find { it.id == reservationId }
    val approved = reservation?.status == ReservationStatus.APPROVED && action != SummaryAction.CANCELLED
    if (approved) {
        ReservationQrScreen(viewModel, reservationId, onBack = onViewBookings, title = "Reservation approved")
        return
    }
    val colors = SolGridTheme.colors
    val (icon, tone) = when (action) {
        SummaryAction.CREATED -> Icons.Outlined.EditCalendar to colors.accent
        SummaryAction.UPDATED -> Icons.Outlined.CheckCircle to colors.success
        SummaryAction.CANCELLED -> Icons.Outlined.Cancel to colors.error
    }

    Column(
        modifier = Modifier.fillMaxSize().background(colors.background),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        AppTopBar(title = "Reservation summary", onBack = onViewBookings)
        Column(
            modifier = Modifier.weight(1f).verticalScroll(rememberScrollState()).padding(Spacing.xxl),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Box(
                modifier = Modifier.size(72.dp).clip(CircleShape).background(tone.copy(alpha = 0.12f)),
                contentAlignment = Alignment.Center
            ) {
                Icon(icon, contentDescription = null, tint = tone, modifier = Modifier.size(36.dp))
            }
            Text(
                action.title,
                style = AppType.screenTitle,
                color = colors.textPrimary,
                textAlign = TextAlign.Center,
                modifier = Modifier.padding(top = Spacing.xl)
            )
            Text(
                action.description,
                style = AppType.body,
                color = colors.textSecondary,
                textAlign = TextAlign.Center,
                modifier = Modifier.padding(top = Spacing.sm)
            )
            reservation?.let { booking ->
                Column(
                    modifier = Modifier.fillMaxWidth().padding(top = Spacing.xl)
                        .clip(RoundedCornerShape(20.dp)).background(colors.surface).padding(Spacing.lg)
                ) {
                    StatusBadge(text = booking.status.name.lowercase().replaceFirstChar { it.uppercase() }, tone = if (booking.status == ReservationStatus.PENDING) BadgeTone.WARNING else BadgeTone.NEUTRAL)
                    Text(booking.nodeName, style = AppType.sectionTitle, color = colors.textPrimary, modifier = Modifier.padding(top = Spacing.md))
                    Text(booking.date, style = AppType.body, color = colors.textPrimary, modifier = Modifier.padding(top = Spacing.sm))
                    Text(booking.startTime, style = AppType.bodyStrong, color = colors.textPrimary)
                    Text("Booking reference", style = AppType.caption, color = colors.textSecondary, modifier = Modifier.padding(top = Spacing.md))
                    Text(booking.id, style = AppType.supporting, color = colors.textPrimary)
                }
                if (booking.status == ReservationStatus.PENDING && action != SummaryAction.CANCELLED) {
                    SecondaryButton(text = if (state.reservationsLoading) "Checking approval…" else "Check approval status", onClick = { viewModel.loadReservations() }, enabled = !state.reservationsLoading, modifier = Modifier.fillMaxWidth().padding(top = Spacing.lg))
                    state.reservationsError?.let { Text(it, style = AppType.caption, color = colors.error, modifier = Modifier.padding(top = Spacing.sm)) }
                    Text("Next: operator approval", style = AppType.bodyStrong, color = colors.textPrimary, modifier = Modifier.padding(top = Spacing.xl))
                    Text("Open this booking after approval to show your secure QR at the station. A pending request cannot be used for check-in.", style = AppType.supporting, color = colors.textSecondary, textAlign = TextAlign.Center, modifier = Modifier.padding(top = Spacing.sm))
                }
            }
        }
        Column(modifier = Modifier.fillMaxWidth().padding(horizontal = Spacing.xxl)) {
            PrimaryButton(text = "View My Bookings", onClick = onViewBookings, modifier = Modifier.fillMaxWidth())
            SecondaryButton(text = "Back to Dashboard", onClick = onBackToDashboard, modifier = Modifier.fillMaxWidth().padding(top = Spacing.md, bottom = Spacing.xl))
        }
    }
}
