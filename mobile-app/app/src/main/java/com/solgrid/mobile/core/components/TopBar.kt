package com.solgrid.mobile.core.components

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.ArrowBack
import androidx.compose.material.icons.outlined.NotificationsNone
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.SolGridTheme

/** Standard screen top bar with optional back button, title, and trailing notification bell. */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AppTopBar(
    title: String,
    modifier: Modifier = Modifier,
    onBack: (() -> Unit)? = null,
    showNotifications: Boolean = false,
    hasUnreadNotifications: Boolean = false,
    onNotificationsClick: () -> Unit = {},
    actions: @Composable () -> Unit = {}
) {
    val colors = SolGridTheme.colors
    TopAppBar(
        title = { Text(title, style = AppType.sectionTitle, color = colors.textPrimary) },
        modifier = modifier,
        navigationIcon = {
            if (onBack != null) {
                IconButton(onClick = onBack) {
                    Box(
                        modifier = Modifier.size(36.dp).clip(CircleShape).background(colors.surface),
                        contentAlignment = Alignment.Center
                    ) {
                        Icon(Icons.AutoMirrored.Outlined.ArrowBack, contentDescription = "Back", tint = colors.textPrimary, modifier = Modifier.size(18.dp))
                    }
                }
            }
        },
        actions = {
            actions()
            if (showNotifications) {
                IconButton(onClick = onNotificationsClick) {
                    Box(contentAlignment = Alignment.TopEnd) {
                        Icon(Icons.Outlined.NotificationsNone, contentDescription = "Notifications", tint = colors.textPrimary)
                        if (hasUnreadNotifications) {
                            Box(
                                modifier = Modifier
                                    .size(8.dp)
                                    .clip(CircleShape)
                                    .background(colors.error)
                            )
                        }
                    }
                }
            }
        },
        colors = TopAppBarDefaults.topAppBarColors(
            containerColor = colors.background,
            scrolledContainerColor = colors.background
        )
    )
}
