package com.solgrid.mobile.feature.prosumer

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
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Bolt
import androidx.compose.material.icons.outlined.EventAvailable
import androidx.compose.material.icons.outlined.EvStation
import androidx.compose.material.icons.outlined.NotificationsNone
import androidx.compose.material.icons.outlined.PendingActions
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.components.AppPullToRefresh
import com.solgrid.mobile.core.components.EmptyKind
import com.solgrid.mobile.core.components.EmptyState
import com.solgrid.mobile.core.components.HeroStatCard
import com.solgrid.mobile.core.components.HomeSkeleton
import com.solgrid.mobile.core.components.SectionHeader
import com.solgrid.mobile.core.components.StatTile
import com.solgrid.mobile.core.components.StatusBadge
import com.solgrid.mobile.core.components.UserAvatar
import com.solgrid.mobile.core.components.clickableNoRipple
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.models.EnergyReservation
import com.solgrid.mobile.feature.reservations.statusLabel
import com.solgrid.mobile.feature.reservations.statusTone

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
            state.reservationsError?.let { message ->
                Text(message, style = AppType.body, color = colors.error)
                com.solgrid.mobile.core.components.SecondaryButton("Retry bookings", { viewModel.loadReservations() })
            }
            Row(
                modifier = Modifier.fillMaxWidth().padding(top = Spacing.sm),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(Spacing.sm)) {
                    UserAvatar(name = state.profile.fullName, modifier = Modifier.clickableNoRipple(onProfileClick))
                    Column {
                        Text(state.profile.fullName, style = AppType.bodyStrong, color = colors.textPrimary)
                        Text("Colombo, Sri Lanka", style = AppType.caption, color = colors.textSecondary)
                    }
                }
                Box(
                    modifier = Modifier.size(40.dp).clip(CircleShape).background(colors.surface),
                    contentAlignment = Alignment.Center
                ) {
                    Icon(Icons.Outlined.NotificationsNone, contentDescription = "Notifications", tint = colors.textPrimary, modifier = Modifier.size(20.dp))
                }
            }

            // Plain hero card mirrors the reference UI set's total-kWh callout, no photo/illustration.
            HeroStatCard(
                statValue = state.summary?.currentReservationsCount?.toString() ?: "—",
                statLabel = "Current Bookings",
                icon = Icons.Outlined.Bolt,
                modifier = Modifier.padding(top = Spacing.lg)
            )

            Row(
                modifier = Modifier.fillMaxWidth().padding(top = Spacing.md),
                horizontalArrangement = Arrangement.spacedBy(Spacing.sm)
            ) {
                StatTile(
                    value = state.summary?.pendingReservationsCount?.toString() ?: "—",
                    label = "Pending",
                    icon = Icons.Outlined.PendingActions,
                    modifier = Modifier.weight(1f)
                )
                StatTile(
                    value = state.summary?.approvedFutureReservationsCount?.toString() ?: "—",
                    label = "Approved",
                    icon = Icons.Outlined.EventAvailable,
                    highlighted = true,
                    modifier = Modifier.weight(1f)
                )
                StatTile(
                    value = state.summary?.bookingHistoryCount?.toString() ?: "—",
                    label = "History",
                    icon = Icons.Outlined.Bolt,
                    modifier = Modifier.weight(1f)
                )
            }

            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(top = Spacing.md)
                    .clip(RoundedCornerShape(Radius.xl))
                    .background(colors.textPrimary)
                    .clickableNoRipple(onFindNodesClick)
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
                    Text("Find a Microgrid Node", style = AppType.bodyStrong, color = colors.background)
                    Text("Browse nearby stations and book a slot", style = AppType.caption, color = colors.background.copy(alpha = 0.7f))
                }
            }
        }

        item {
            SectionHeader(
                title = "Current & Pending Bookings",
                actionText = "View all",
                onActionClick = onViewAllBookings,
                modifier = Modifier.padding(top = Spacing.xl)
            )
        }

        if (state.currentAndPending.isEmpty()) {
            item { EmptyState(kind = EmptyKind.ACTIVITY, modifier = Modifier.padding(top = Spacing.md)) }
        } else {
            items(state.currentAndPending, key = { it.id }) { reservation ->
                ReservationRow(reservation = reservation, onClick = { onBookingClick(reservation.id) })
            }
        }

        item { Box(modifier = Modifier.padding(bottom = Spacing.xxxl)) }
    }
    }
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
