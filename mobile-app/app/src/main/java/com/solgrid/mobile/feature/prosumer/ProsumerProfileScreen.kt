package com.solgrid.mobile.feature.prosumer

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.Logout
import androidx.compose.material.icons.outlined.Badge
import androidx.compose.material.icons.outlined.Email
import androidx.compose.material.icons.automirrored.outlined.HelpOutline
import androidx.compose.material.icons.outlined.PersonOff
import androidx.compose.material.icons.outlined.Phone
import androidx.compose.material.icons.outlined.Settings
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import com.solgrid.mobile.core.components.AppDivider
import com.solgrid.mobile.core.components.BadgeTone
import com.solgrid.mobile.core.components.ConfirmationDialog
import com.solgrid.mobile.core.components.ListRow
import com.solgrid.mobile.core.components.SecondaryButton
import com.solgrid.mobile.core.components.SectionHeader
import com.solgrid.mobile.core.components.StatusBadge
import com.solgrid.mobile.core.components.UserAvatar
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Sizing
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.models.ProsumerAccountStatus

@Composable
fun ProsumerProfileScreen(
    viewModel: ProsumerViewModel,
    onEditProfile: () -> Unit,
    onDeactivationRequest: () -> Unit,
    onSettings: () -> Unit,
    onHelp: () -> Unit,
    onLogout: () -> Unit
) {
    val colors = SolGridTheme.colors
    val state by viewModel.uiState.collectAsState()
    var showLogoutConfirm by remember { mutableStateOf(false) }

    Column(
        modifier = Modifier.fillMaxSize().background(colors.background).verticalScroll(rememberScrollState()).padding(horizontal = Spacing.lg)
    ) {
        Column(modifier = Modifier.fillMaxWidth().padding(top = Spacing.xxl), horizontalAlignment = Alignment.CenterHorizontally) {
            UserAvatar(name = state.profile.fullName, size = Sizing.avatarLg)
            Text(state.profile.fullName, style = AppType.sectionTitle, color = colors.textPrimary, modifier = Modifier.padding(top = Spacing.md))
            Text("NIC: ${state.profile.nic}", style = AppType.supporting, color = colors.textSecondary)
            StatusBadge(
                text = when (state.profile.status) {
                    ProsumerAccountStatus.ACTIVE -> "Active"
                    ProsumerAccountStatus.PENDING -> "Pending Activation"
                    ProsumerAccountStatus.DEACTIVATION_REQUESTED -> "Deactivation Requested"
                    ProsumerAccountStatus.DEACTIVATED -> "Deactivated"
                },
                tone = when (state.profile.status) {
                    ProsumerAccountStatus.ACTIVE -> BadgeTone.SUCCESS
                    ProsumerAccountStatus.PENDING -> BadgeTone.WARNING
                    ProsumerAccountStatus.DEACTIVATION_REQUESTED -> BadgeTone.WARNING
                    ProsumerAccountStatus.DEACTIVATED -> BadgeTone.ERROR
                },
                modifier = Modifier.padding(top = Spacing.sm)
            )
            SecondaryButton(text = "Edit Profile", onClick = onEditProfile, modifier = Modifier.padding(top = Spacing.lg))
        }

        SectionHeader(title = "Contact Information", modifier = Modifier.padding(top = Spacing.xxl))
        ListRow(title = "Email", subtitle = state.profile.email, icon = Icons.Outlined.Email)
        AppDivider()
        ListRow(title = "Phone", subtitle = state.profile.phone, icon = Icons.Outlined.Phone)
        AppDivider()
        ListRow(title = "NIC", subtitle = state.profile.nic, icon = Icons.Outlined.Badge)

        SectionHeader(title = "Account", modifier = Modifier.padding(top = Spacing.xl))
        ListRow(title = "Settings", icon = Icons.Outlined.Settings, showChevron = true, onClick = onSettings)
        AppDivider()
        ListRow(title = "Help & Support", icon = Icons.AutoMirrored.Outlined.HelpOutline, showChevron = true, onClick = onHelp)
        if (state.profile.status == ProsumerAccountStatus.ACTIVE) {
            AppDivider()
            ListRow(
                title = "Request Account Deactivation",
                icon = Icons.Outlined.PersonOff,
                iconTint = colors.warning,
                showChevron = true,
                onClick = onDeactivationRequest
            )
        }

        ListRow(
            title = "Log Out",
            icon = Icons.AutoMirrored.Outlined.Logout,
            iconTint = colors.error,
            modifier = Modifier.padding(top = Spacing.xl, bottom = Spacing.huge),
            onClick = { showLogoutConfirm = true }
        )
    }

    if (showLogoutConfirm) {
        ConfirmationDialog(
            title = "Log out?",
            message = "You'll need to sign in again to access your account.",
            confirmText = "Log Out",
            destructive = true,
            onConfirm = { showLogoutConfirm = false; onLogout() },
            onDismiss = { showLogoutConfirm = false }
        )
    }
}
