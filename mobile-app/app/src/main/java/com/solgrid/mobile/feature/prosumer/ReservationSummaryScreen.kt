package com.solgrid.mobile.feature.prosumer

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
    CREATED("Reservation Requested", "Your booking is pending approval. You'll be notified once a Grid Operator confirms it."),
    UPDATED("Reservation Updated", "Your changes have been saved and sent for confirmation."),
    CANCELLED("Reservation Cancelled", "This booking has been cancelled and the slot released.")
}

/** Shown after create/update/cancel actions (MOB-10). */
@Composable
fun ReservationSummaryScreen(
    action: SummaryAction,
    onViewBookings: () -> Unit,
    onBackToDashboard: () -> Unit
) {
    val colors = SolGridTheme.colors
    val (icon, tone) = when (action) {
        SummaryAction.CREATED -> Icons.Outlined.EditCalendar to colors.accent
        SummaryAction.UPDATED -> Icons.Outlined.CheckCircle to colors.success
        SummaryAction.CANCELLED -> Icons.Outlined.Cancel to colors.error
    }

    Column(
        modifier = Modifier.fillMaxSize().background(colors.background).padding(Spacing.xxl),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Box(modifier = Modifier.weight(1f))
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
        Box(modifier = Modifier.weight(1f))

        PrimaryButton(text = "View My Bookings", onClick = onViewBookings, modifier = Modifier.fillMaxWidth())
        SecondaryButton(text = "Back to Dashboard", onClick = onBackToDashboard, modifier = Modifier.fillMaxWidth().padding(top = Spacing.md, bottom = Spacing.xl))
    }
}
