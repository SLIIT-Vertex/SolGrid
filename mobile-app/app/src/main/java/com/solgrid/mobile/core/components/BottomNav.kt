package com.solgrid.mobile.core.components

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.selection.selectable
import androidx.compose.foundation.selection.selectableGroup
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
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.Sizing
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.navigation.ProsumerBottomNav

/**
 * Floating bottom navigation on a white surface with the brand-green active tab. Every tab shows its label
 * under the icon, because icon-only tabs left users guessing what each one opened.
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
            .padding(horizontal = 16.dp, vertical = 10.dp)
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .height(72.dp)
                .clip(RoundedCornerShape(Radius.xl))
                .background(colors.surface)
                .border(Sizing.borderThin, colors.border, RoundedCornerShape(Radius.xl))
                .padding(horizontal = 6.dp)
                .selectableGroup(),
            horizontalArrangement = Arrangement.SpaceEvenly,
            verticalAlignment = Alignment.CenterVertically
        ) {
            ProsumerBottomNav.entries.forEach { dest ->
                val selected = dest == current
                NavTabItem(
                    icon = iconFor(dest, selected),
                    label = dest.label,
                    selected = selected,
                    onClick = { onSelect(dest) }
                )
            }
        }
    }
}

@Composable
private fun RowScope.NavTabItem(icon: ImageVector, label: String, selected: Boolean, onClick: () -> Unit) {
    val colors = SolGridTheme.colors
    val labelColor = if (selected) colors.accent else colors.textSecondary
    Column(
        modifier = Modifier
            .weight(1f)
            .fillMaxHeight()
            .clip(RoundedCornerShape(Radius.lg))
            // The label Text below is the accessible name, so the icon stays decorative.
            .selectable(selected = selected, onClick = onClick, role = Role.Tab),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Box(
            modifier = Modifier
                .width(52.dp)
                .height(30.dp)
                .clip(RoundedCornerShape(Radius.pill))
                .background(if (selected) colors.accent else Color.Transparent),
            contentAlignment = Alignment.Center
        ) {
            Icon(
                icon,
                contentDescription = null,
                tint = if (selected) colors.onAccent else colors.textSecondary,
                modifier = Modifier.size(20.dp)
            )
        }
        Text(
            text = label,
            style = AppType.caption,
            fontWeight = if (selected) FontWeight.SemiBold else FontWeight.Medium,
            color = labelColor,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis,
            modifier = Modifier.padding(top = 4.dp)
        )
    }
}

private fun iconFor(dest: ProsumerBottomNav, selected: Boolean): ImageVector = when (dest) {
    ProsumerBottomNav.DASHBOARD -> if (selected) Icons.Filled.Bolt else Icons.Outlined.Bolt
    ProsumerBottomNav.MAP -> if (selected) Icons.Filled.EvStation else Icons.Outlined.EvStation
    ProsumerBottomNav.BOOKINGS -> if (selected) Icons.Filled.EventAvailable else Icons.Outlined.EventAvailable
    ProsumerBottomNav.PROFILE -> if (selected) Icons.Filled.PersonPin else Icons.Outlined.PersonPin
}
