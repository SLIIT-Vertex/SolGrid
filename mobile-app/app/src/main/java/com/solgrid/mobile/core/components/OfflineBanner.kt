package com.solgrid.mobile.core.components

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.expandVertically
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.shrinkVertically
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.CloudOff
import androidx.compose.material.icons.outlined.CloudQueue
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing

/** Subtle connectivity banner. Does not block the app — just informs. */
@Composable
fun ConnectivityBanner(state: ConnectivityState, modifier: Modifier = Modifier) {
    val colors = SolGridTheme.colors
    AnimatedVisibility(
        visible = state != ConnectivityState.ONLINE_IDLE,
        enter = fadeIn() + expandVertically(),
        exit = fadeOut() + shrinkVertically()
    ) {
        val (bg, fg, icon, text) = when (state) {
            ConnectivityState.OFFLINE -> BannerStyle(colors.errorSurface, colors.error, Icons.Outlined.CloudOff, "You're offline — some actions may be unavailable.")
            ConnectivityState.BACK_ONLINE -> BannerStyle(colors.successSurface, colors.success, Icons.Outlined.CloudQueue, "Back online")
            ConnectivityState.ONLINE_IDLE -> BannerStyle(colors.surface, colors.textSecondary, Icons.Outlined.CloudQueue, "")
        }
        Row(
            modifier = modifier.fillMaxWidth().background(bg).padding(horizontal = Spacing.lg, vertical = Spacing.sm),
            horizontalArrangement = Arrangement.spacedBy(Spacing.sm),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Icon(icon, contentDescription = null, tint = fg, modifier = Modifier.size(16.dp))
            Text(text, style = AppType.supporting, color = fg)
        }
    }
}

enum class ConnectivityState { ONLINE_IDLE, OFFLINE, BACK_ONLINE }

private data class BannerStyle(
    val bg: androidx.compose.ui.graphics.Color,
    val fg: androidx.compose.ui.graphics.Color,
    val icon: androidx.compose.ui.graphics.vector.ImageVector,
    val text: String
)
