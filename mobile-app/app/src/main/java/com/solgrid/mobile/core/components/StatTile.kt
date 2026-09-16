package com.solgrid.mobile.core.components

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.vector.ImageVector
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing

/**
 * Light-gray rounded stat rectangle matching the reference UI set's "Capacity / Consumed / Used
 * in battery" tiles — a compact value + label pair, optionally with a small leading icon.
 */
@Composable
fun StatTile(
    value: String,
    label: String,
    modifier: Modifier = Modifier,
    icon: ImageVector? = null,
    highlighted: Boolean = false
) {
    val colors = SolGridTheme.colors
    Column(
        modifier = modifier
            .clip(RoundedCornerShape(Radius.lg))
            .background(if (highlighted) colors.accentSurface else colors.surfaceAlt)
            .padding(Spacing.md)
    ) {
        if (icon != null) {
            Icon(
                icon,
                contentDescription = null,
                tint = if (highlighted) colors.accent else colors.textSecondary,
                modifier = Modifier.padding(bottom = Spacing.xs)
            )
        }
        Text(value, style = AppType.bodyStrong, color = if (highlighted) colors.accent else colors.textPrimary)
        Text(label, style = AppType.caption, color = if (highlighted) colors.accent.copy(alpha = 0.75f) else colors.textSecondary)
    }
}
