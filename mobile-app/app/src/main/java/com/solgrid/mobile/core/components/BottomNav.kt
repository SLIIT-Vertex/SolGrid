package com.solgrid.mobile.core.components

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Bolt
import androidx.compose.material.icons.filled.EvStation
import androidx.compose.material.icons.filled.EventAvailable
import androidx.compose.material.icons.filled.PersonPin
import androidx.compose.material.icons.outlined.Bolt
import androidx.compose.material.icons.outlined.EvStation
import androidx.compose.material.icons.outlined.EventAvailable
import androidx.compose.material.icons.outlined.PersonPin
import androidx.compose.material3.Icon
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.navigation.ProsumerBottomNav

/**
 * Floating pill-style bottom navigation (dark bar, single highlighted active icon) matching the
 * reference UI's nav treatment, rebuilt with our navy/copper palette instead of a stock
 * Material NavigationBar.
 */
@Composable
fun ProsumerBottomNavigation(
    current: ProsumerBottomNav,
    onSelect: (ProsumerBottomNav) -> Unit
) {
    val colors = SolGridTheme.colors
    Box(
        modifier = Modifier
            .fillMaxWidth()
            .background(colors.background)
            .padding(horizontal = 20.dp, vertical = 12.dp)
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .height(64.dp)
                .clip(RoundedCornerShape(Radius.pill))
                .background(colors.textPrimary)
                .padding(horizontal = 8.dp),
            horizontalArrangement = androidx.compose.foundation.layout.Arrangement.SpaceEvenly,
            verticalAlignment = Alignment.CenterVertically
        ) {
            ProsumerBottomNav.entries.forEach { dest ->
                val selected = dest == current
                NavPillItem(
                    icon = iconFor(dest, selected),
                    selected = selected,
                    onClick = { onSelect(dest) }
                )
            }
        }
    }
}

@Composable
private fun NavPillItem(icon: ImageVector, selected: Boolean, onClick: () -> Unit) {
    val colors = SolGridTheme.colors
    Box(
        modifier = Modifier
            .size(if (selected) 46.dp else 40.dp)
            .clip(CircleShape)
            .background(if (selected) colors.accent else androidx.compose.ui.graphics.Color.Transparent)
            .clickableNoRipple(onClick),
        contentAlignment = Alignment.Center
    ) {
        Icon(
            icon,
            contentDescription = null,
            tint = if (selected) colors.onAccent else colors.textTertiary.copy(alpha = 0.9f),
            modifier = Modifier.size(20.dp)
        )
    }
}

private fun iconFor(dest: ProsumerBottomNav, selected: Boolean): ImageVector = when (dest) {
    ProsumerBottomNav.DASHBOARD -> if (selected) Icons.Filled.Bolt else Icons.Outlined.Bolt
    ProsumerBottomNav.MAP -> if (selected) Icons.Filled.EvStation else Icons.Outlined.EvStation
    ProsumerBottomNav.BOOKINGS -> if (selected) Icons.Filled.EventAvailable else Icons.Outlined.EventAvailable
    ProsumerBottomNav.PROFILE -> if (selected) Icons.Filled.PersonPin else Icons.Outlined.PersonPin
}
