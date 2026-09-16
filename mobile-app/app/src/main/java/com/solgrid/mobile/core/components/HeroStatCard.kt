package com.solgrid.mobile.core.components

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing

/**
 * Plain solid hero card with a "total energy" style stat, matching the reference UI set's
 * total-kWh callout but without a photo/illustration background — a flat ink-colored card with
 * the stat and an icon badge, reading live numbers from state.
 */
@Composable
fun HeroStatCard(
    statValue: String,
    statLabel: String,
    icon: ImageVector,
    modifier: Modifier = Modifier
) {
    val colors = SolGridTheme.colors
    Column(
        modifier = modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(Radius.xl))
            .background(colors.textPrimary)
            .padding(Spacing.lg)
    ) {
        Box(
            modifier = Modifier.size(44.dp).clip(CircleShape).background(colors.accent),
            contentAlignment = Alignment.Center
        ) {
            Icon(icon, contentDescription = null, tint = colors.onAccent)
        }
        Text(statValue, style = AppType.screenTitle, color = colors.background, modifier = Modifier.padding(top = Spacing.lg))
        Text(statLabel, style = AppType.supporting, color = colors.background.copy(alpha = 0.7f))
    }
}

/**
 * Horizontally-laid-out row of circular icon toggles (the reference UI's "select a device/room"
 * pattern) — used here for quick filters like node categories or booking states.
 */
@Composable
fun IconToggleRow(
    items: List<IconToggleItem>,
    selectedId: String,
    onSelect: (String) -> Unit,
    modifier: Modifier = Modifier
) {
    Row(modifier = modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
        items.forEach { item ->
            IconToggle(item = item, selected = item.id == selectedId, onClick = { onSelect(item.id) })
        }
    }
}

data class IconToggleItem(
    val id: String,
    val icon: ImageVector,
    val label: String
)

@Composable
private fun IconToggle(item: IconToggleItem, selected: Boolean, onClick: () -> Unit) {
    val colors = SolGridTheme.colors
    Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.clickableNoRipple(onClick)) {
        Box(
            modifier = Modifier
                .size(52.dp)
                .clip(CircleShape)
                .background(if (selected) colors.accent else colors.surface),
            contentAlignment = Alignment.Center
        ) {
            Icon(
                item.icon,
                contentDescription = item.label,
                tint = if (selected) colors.onAccent else colors.textSecondary,
                modifier = Modifier.size(22.dp)
            )
        }
    }
}
