package com.solgrid.mobile.feature.prosumer

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.ArrowForward
import androidx.compose.material.icons.outlined.Bolt
import androidx.compose.material.icons.outlined.ErrorOutline
import androidx.compose.material.icons.outlined.EvStation
import androidx.compose.material.icons.outlined.EventAvailable
import androidx.compose.material.icons.outlined.History
import androidx.compose.material.icons.outlined.HourglassTop
import androidx.compose.material.icons.outlined.PendingActions
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.solgrid.mobile.core.components.AppDivider
import com.solgrid.mobile.core.components.AppPullToRefresh
import com.solgrid.mobile.core.components.EmptyKind
import com.solgrid.mobile.core.components.EmptyState
import com.solgrid.mobile.core.components.HomeSkeleton
import com.solgrid.mobile.core.components.SecondaryButton
import com.solgrid.mobile.core.components.SectionHeader
import com.solgrid.mobile.core.components.StatusBadge
import com.solgrid.mobile.core.components.UserAvatar
import com.solgrid.mobile.core.components.clickableNoRipple
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.CopperDeep
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.design.Sizing
import com.solgrid.mobile.core.models.EnergyReservation
import com.solgrid.mobile.core.models.ProsumerAccountStatus
import com.solgrid.mobile.feature.reservations.statusLabel
import com.solgrid.mobile.feature.reservations.statusTone
import java.time.LocalTime

@Composable
fun DashboardScreen(
    viewModel: ProsumerViewModel,
    onProfileClick: () -> Unit,
    onFindNodesClick: () -> Unit,
    onBookingClick: (String) -> Unit,
    onViewAllBookings: () -> Unit
) {
    val colors = SolGridTheme.colors
    val state by viewModel.uiState.collectAsState()

    if (state.loading) {
        Box(modifier = Modifier.fillMaxSize().background(colors.background).padding(Spacing.lg)) {
            HomeSkeleton()
        }
        return
    }

    val summary = state.summary

    AppPullToRefresh(
        refreshing = state.reservationsLoading && !state.loading,
        onRefresh = { viewModel.loadReservations() },
        modifier = Modifier.fillMaxSize().background(colors.background)
    ) {
        LazyColumn(
            modifier = Modifier.fillMaxSize(),
            contentPadding = PaddingValues(horizontal = Spacing.lg, vertical = Spacing.md)
        ) {
            item {
                DashboardHeader(
                    firstName = state.profile.firstName,
                    fullName = state.profile.fullName,
                    onProfileClick = onProfileClick
                )
            }

            if (state.profile.status == ProsumerAccountStatus.PENDING) {
                item {
                    NoticeCard(
                        icon = Icons.Outlined.HourglassTop,
                        title = "Account awaiting approval",
                        message = "A Backoffice officer needs to activate your account before you can book a slot.",
                        modifier = Modifier.padding(top = Spacing.lg)
                    )
                }
            }

            state.profileError?.let { message ->
                item {
                    NoticeCard(
                        icon = Icons.Outlined.ErrorOutline,
                        title = "Couldn't load your profile",
                        message = message,
                        isError = true,
                        actionText = "Try again",
                        onAction = { viewModel.loadProfile() },
                        modifier = Modifier.padding(top = Spacing.lg)
                    )
                }
            }

            state.reservationsError?.let { message ->
                item {
                    NoticeCard(
                        icon = Icons.Outlined.ErrorOutline,
                        title = "Couldn't load your bookings",
                        message = message,
                        isError = true,
                        actionText = "Try again",
                        onAction = { viewModel.loadReservations() },
                        modifier = Modifier.padding(top = Spacing.lg)
                    )
                }
            }

            item {
                SummaryHeroCard(
                    currentCount = summary?.currentReservationsCount,
                    upcomingCount = summary?.approvedFutureReservationsCount,
                    modifier = Modifier.padding(top = Spacing.lg)
                )

                Row(
                    modifier = Modifier.fillMaxWidth().padding(top = Spacing.md),
                    horizontalArrangement = Arrangement.spacedBy(Spacing.sm)
                ) {
                    DashboardStatCard(
                        value = summary?.pendingReservationsCount,
                        label = "Pending",
                        icon = Icons.Outlined.PendingActions,
                        iconTint = colors.warning,
                        iconBackground = colors.warningSurface,
                        onClick = onViewAllBookings,
                        modifier = Modifier.weight(1f)
                    )
                    DashboardStatCard(
                        value = summary?.approvedFutureReservationsCount,
                        label = "Approved",
                        icon = Icons.Outlined.EventAvailable,
                        iconTint = colors.accent,
                        iconBackground = colors.accentSurface,
                        onClick = onViewAllBookings,
                        modifier = Modifier.weight(1f)
                    )
                    DashboardStatCard(
                        value = summary?.bookingHistoryCount,
                        label = "History",
                        icon = Icons.Outlined.History,
                        iconTint = colors.textSecondary,
                        iconBackground = colors.surfaceAlt,
                        onClick = onViewAllBookings,
                        modifier = Modifier.weight(1f)
                    )
                }

                FindNodeCard(onClick = onFindNodesClick, modifier = Modifier.padding(top = Spacing.md))
            }

            item {
                SectionHeader(
                    title = "Current & pending bookings",
                    actionText = "View all",
                    onActionClick = onViewAllBookings,
                    modifier = Modifier.padding(top = Spacing.xl)
                )
            }

            item {
                if (state.currentAndPending.isEmpty()) {
                    EmptyState(kind = EmptyKind.ACTIVITY, modifier = Modifier.padding(top = Spacing.sm))
                } else {
                    Column(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(top = Spacing.xs)
                            .clip(RoundedCornerShape(Radius.lg))
                            .background(colors.surface)
                            .border(Sizing.borderThin, colors.border, RoundedCornerShape(Radius.lg))
                            .padding(horizontal = Spacing.md)
                    ) {
                        state.currentAndPending.forEachIndexed { index, reservation ->
                            if (index > 0) AppDivider()
                            ReservationRow(reservation = reservation, onClick = { onBookingClick(reservation.id) })
                        }
                    }
                }
            }

            item { Box(modifier = Modifier.padding(bottom = Spacing.xxxl)) }
        }
    }
}

@Composable
private fun DashboardHeader(firstName: String, fullName: String, onProfileClick: () -> Unit) {
    val colors = SolGridTheme.colors
    val greeting = remember { greetingFor(LocalTime.now()) }
    Row(
        modifier = Modifier.fillMaxWidth().padding(top = Spacing.sm),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Column(modifier = Modifier.weight(1f)) {
            Text(greeting, style = AppType.supporting, color = colors.textSecondary)
            Text(
                text = firstName.ifBlank { "Welcome back" },
                style = AppType.screenTitle,
                color = colors.textPrimary,
                modifier = Modifier.semantics { heading() }
            )
        }
        UserAvatar(
            name = fullName,
            modifier = Modifier
                .clip(CircleShape)
                .clickable(role = Role.Button, onClickLabel = "Open profile", onClick = onProfileClick)
        )
    }
}

/**
 * Brand-green summary card. Drawn from the accent palette rather than textPrimary, so it keeps
 * the same look in dark mode instead of flipping to a cream block.
 */
@Composable
private fun SummaryHeroCard(currentCount: Long?, upcomingCount: Long?, modifier: Modifier = Modifier) {
    val colors = SolGridTheme.colors
    val onHero = Color.White
    val message = when {
        currentCount == null -> "Pull down to refresh your bookings."
        currentCount > 0 -> "You have an energy slot in progress right now."
        (upcomingCount ?: 0) > 0 -> "Next up: $upcomingCount approved ${if (upcomingCount == 1L) "booking" else "bookings"}."
        else -> "No active bookings. Find a node to reserve a slot."
    }

    Column(
        modifier = modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(Radius.xl))
            .background(Brush.linearGradient(listOf(colors.accent, CopperDeep)))
            .padding(Spacing.lg)
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Text(
                "Current bookings",
                style = AppType.bodyStrong,
                color = onHero.copy(alpha = 0.9f),
                modifier = Modifier.weight(1f)
            )
            Box(
                modifier = Modifier.size(40.dp).clip(CircleShape).background(onHero.copy(alpha = 0.18f)),
                contentAlignment = Alignment.Center
            ) {
                Icon(Icons.Outlined.Bolt, contentDescription = null, tint = onHero, modifier = Modifier.size(22.dp))
            }
        }
        Text(
            text = currentCount?.toString() ?: "—",
            color = onHero,
            fontSize = 40.sp,
            lineHeight = 44.sp,
            fontWeight = FontWeight.SemiBold,
            modifier = Modifier.padding(top = Spacing.xs)
        )
        Box(
            modifier = Modifier
                .padding(vertical = Spacing.md)
                .fillMaxWidth()
                .height(Sizing.borderThin)
                .background(onHero.copy(alpha = 0.2f))
        )
        Text(message, style = AppType.supporting, color = onHero.copy(alpha = 0.85f))
    }
}

@Composable
private fun DashboardStatCard(
    value: Long?,
    label: String,
    icon: ImageVector,
    iconTint: Color,
    iconBackground: Color,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    val colors = SolGridTheme.colors
    Column(
        modifier = modifier
            .clip(RoundedCornerShape(Radius.lg))
            .background(colors.surface)
            .border(Sizing.borderThin, colors.border, RoundedCornerShape(Radius.lg))
            .clickable(role = Role.Button, onClickLabel = "View $label bookings", onClick = onClick)
            .padding(Spacing.md)
    ) {
        Box(
            modifier = Modifier.size(36.dp).clip(RoundedCornerShape(Radius.md)).background(iconBackground),
            contentAlignment = Alignment.Center
        ) {
            Icon(icon, contentDescription = null, tint = iconTint, modifier = Modifier.size(20.dp))
        }
        Text(
            text = value?.toString() ?: "—",
            style = AppType.sectionTitle,
            color = colors.textPrimary,
            modifier = Modifier.padding(top = Spacing.sm)
        )
        Text(label, style = AppType.caption, color = colors.textSecondary)
    }
}

@Composable
private fun FindNodeCard(onClick: () -> Unit, modifier: Modifier = Modifier) {
    val colors = SolGridTheme.colors
    Row(
        modifier = modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(Radius.xl))
            .background(colors.accentSurface)
            .clickable(role = Role.Button, onClick = onClick)
            .padding(Spacing.lg),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(Spacing.md)
    ) {
        Box(
            modifier = Modifier.size(44.dp).clip(CircleShape).background(colors.accent),
            contentAlignment = Alignment.Center
        ) {
            Icon(Icons.Outlined.EvStation, contentDescription = null, tint = colors.onAccent)
        }
        Column(modifier = Modifier.weight(1f)) {
            Text("Find a microgrid node", style = AppType.bodyStrong, color = colors.textPrimary)
            Text("Browse nearby stations and book a slot", style = AppType.caption, color = colors.textSecondary)
        }
        Icon(
            Icons.AutoMirrored.Outlined.ArrowForward,
            contentDescription = null,
            tint = colors.accent,
            modifier = Modifier.size(20.dp)
        )
    }
}

@Composable
private fun NoticeCard(
    icon: ImageVector,
    title: String,
    message: String,
    modifier: Modifier = Modifier,
    isError: Boolean = false,
    actionText: String? = null,
    onAction: (() -> Unit)? = null
) {
    val colors = SolGridTheme.colors
    Column(
        modifier = modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(Radius.lg))
            .background(if (isError) colors.errorSurface else colors.warningSurface)
            .padding(Spacing.md)
    ) {
        Row(horizontalArrangement = Arrangement.spacedBy(Spacing.sm)) {
            Icon(
                icon,
                contentDescription = null,
                tint = if (isError) colors.error else colors.warning,
                modifier = Modifier.size(20.dp)
            )
            Column(modifier = Modifier.weight(1f)) {
                Text(title, style = AppType.bodyStrong, color = colors.textPrimary)
                Text(message, style = AppType.supporting, color = colors.textSecondary)
            }
        }
        if (actionText != null && onAction != null) {
            SecondaryButton(actionText, onAction, modifier = Modifier.padding(top = Spacing.sm))
        }
    }
}

private fun greetingFor(time: LocalTime): String = when (time.hour) {
    in 5..11 -> "Good morning"
    in 12..16 -> "Good afternoon"
    else -> "Good evening"
}

@Composable
fun ReservationRow(reservation: EnergyReservation, onClick: () -> Unit) {
    val colors = SolGridTheme.colors
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .clickableNoRipple(onClick)
            .padding(vertical = Spacing.md),
        horizontalArrangement = Arrangement.spacedBy(Spacing.md),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Box(
            modifier = Modifier.size(44.dp).clip(RoundedCornerShape(Radius.md)).background(colors.accentSurface),
            contentAlignment = Alignment.Center
        ) {
            Icon(Icons.Outlined.Bolt, contentDescription = null, tint = colors.accent)
        }
        Column(modifier = Modifier.weight(1f)) {
            Text(reservation.nodeName, style = AppType.bodyStrong, color = colors.textPrimary)
            Text("${reservation.date} · ${reservation.startTime}-${reservation.endTime}", style = AppType.supporting, color = colors.textSecondary)
        }
        StatusBadge(text = statusLabel(reservation.status), tone = statusTone(reservation.status))
    }
}
