package com.solgrid.mobile.feature.operator

import androidx.compose.foundation.clickable
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.Logout
import androidx.compose.material.icons.outlined.QrCodeScanner
import androidx.compose.material3.Icon
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
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.components.AppDivider
import com.solgrid.mobile.core.components.AppPullToRefresh
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.ConfirmationDialog
import com.solgrid.mobile.core.components.PrimaryButton
import com.solgrid.mobile.core.components.SecondaryButton
import com.solgrid.mobile.core.components.SectionHeader
import com.solgrid.mobile.core.components.StatTile
import com.solgrid.mobile.core.components.UserAvatar
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.models.ReservationStatus
import com.solgrid.mobile.feature.prosumer.ReservationRow
import androidx.compose.material.icons.outlined.EventAvailable
import androidx.compose.material.icons.outlined.TaskAlt

/** Grid Operator's mobile home — operational tools only, no Backoffice admin functions. */
@Composable
fun OperatorHomeScreen(
    viewModel: OperatorViewModel,
    onScanClick: () -> Unit,
    onBookingClick: (String) -> Unit,
    onNodesClick: () -> Unit,
    onLogout: () -> Unit
) {
    val colors = SolGridTheme.colors
    val state by viewModel.uiState.collectAsState()
    var showLogoutConfirm by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) { viewModel.loadQueue() }

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(
            title = "Operator Mode",
            actions = {
                androidx.compose.material3.IconButton(onClick = { showLogoutConfirm = true }) {
                    Icon(Icons.AutoMirrored.Outlined.Logout, contentDescription = "Log out", tint = colors.textPrimary)
                }
            }
        )
        AppPullToRefresh(
            refreshing = state.queueLoading && state.queue.isNotEmpty(),
            onRefresh = { viewModel.loadQueue() },
            modifier = Modifier.fillMaxSize()
        ) {
        Column(modifier = Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = Spacing.lg)) {
            Row(
                modifier = Modifier.fillMaxWidth().padding(top = Spacing.md),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(Spacing.md)
            ) {
                UserAvatar(name = state.profile.fullName)
                Column {
                    Text(state.profile.fullName, style = AppType.sectionTitle, color = colors.textPrimary)
                    Text(state.profile.assignedNodeName, style = AppType.supporting, color = colors.textSecondary)
                }
            }

            Row(
                modifier = Modifier.fillMaxWidth().padding(top = Spacing.lg),
                horizontalArrangement = Arrangement.spacedBy(Spacing.sm)
            ) {
                StatTile(
                    value = state.queue.count { it.status == ReservationStatus.APPROVED }.toString(),
                    label = "Ready to Verify",
                    icon = Icons.Outlined.TaskAlt,
                    highlighted = true,
                    modifier = Modifier.weight(1f)
                )
                StatTile(
                    value = state.queue.count { it.status == ReservationStatus.PENDING }.toString(),
                    label = "Pending requests",
                    icon = Icons.Outlined.EventAvailable,
                    modifier = Modifier.weight(1f)
                )
            }

            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(top = Spacing.xl)
                    .clip(RoundedCornerShape(Radius.lg))
                    .background(colors.accent)
                    .clickable(role = Role.Button, onClickLabel = "Open QR scanner", onClick = onScanClick)
                    .padding(Spacing.lg),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(Spacing.md)
            ) {
                Box(
                    modifier = Modifier.size(44.dp).clip(CircleShape).background(colors.onAccent.copy(alpha = 0.16f)),
                    contentAlignment = Alignment.Center
                ) {
                    Icon(Icons.Outlined.QrCodeScanner, contentDescription = null, tint = colors.onAccent)
                }
                Column(modifier = Modifier.weight(1f)) {
                    Text("Scan Prosumer QR", style = AppType.bodyStrong, color = colors.onAccent)
                    Text("Verify and finalize an energy transfer", style = AppType.caption, color = colors.onAccent.copy(alpha = 0.85f))
                }
            }
            PrimaryButton(text = "Browse grid nodes", onClick = onNodesClick, modifier = Modifier.fillMaxWidth().padding(top = Spacing.sm))

            SectionHeader(title = "Grid booking queue", modifier = Modifier.padding(top = Spacing.xl))
            if (state.queueLoading) Text("Loading bookings…", style = AppType.body)
            state.queueError?.let { Text(it, style = AppType.body, color = colors.error); SecondaryButton("Retry", { viewModel.loadQueue() }) }
            Column {
                state.queue.forEach { reservation ->
                    ReservationRow(reservation = reservation, onClick = { onBookingClick(reservation.id) })
                    AppDivider()
                }
            }
            Box(modifier = Modifier.padding(bottom = Spacing.xxxl))
        }
        }
    }

    if (showLogoutConfirm) {
        ConfirmationDialog(
            title = "Log out?",
            message = "You'll need to sign in again to access Operator Mode.",
            confirmText = "Log Out",
            destructive = true,
            onConfirm = { showLogoutConfirm = false; onLogout() },
            onDismiss = { showLogoutConfirm = false }
        )
    }
}
