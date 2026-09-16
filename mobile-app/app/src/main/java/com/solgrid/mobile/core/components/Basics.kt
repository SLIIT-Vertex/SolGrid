package com.solgrid.mobile.core.components

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.Sizing
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing

/** Lightweight click modifier for small text/chip actions. */
fun Modifier.clickableNoRipple(onClick: () -> Unit): Modifier = this.clickable(onClick = onClick)

/** Circular avatar showing an image (when available) or initials. */
@Composable
fun UserAvatar(
    name: String,
    modifier: Modifier = Modifier,
    size: Dp = Sizing.avatarMd,
    imageUrl: String? = null
) {
    val colors = SolGridTheme.colors
    val initials = remember(name) {
        name.trim().split(" ").filter { it.isNotBlank() }.take(2).joinToString("") { it.first().uppercase() }
    }
    Box(
        modifier = modifier
            .size(size)
            .clip(CircleShape)
            .background(colors.accentSurface),
        contentAlignment = Alignment.Center
    ) {
        Text(
            text = initials.ifBlank { "?" },
            style = AppType.bodyStrong,
            color = colors.accent
        )
    }
}

enum class BadgeTone { NEUTRAL, ACCENT, SUCCESS, WARNING, ERROR }

@Composable
fun StatusBadge(
    text: String,
    tone: BadgeTone,
    modifier: Modifier = Modifier
) {
    val colors = SolGridTheme.colors
    val (bg, fg) = when (tone) {
        BadgeTone.NEUTRAL -> colors.surfaceAlt to colors.textSecondary
        BadgeTone.ACCENT -> colors.accentSurface to colors.accent
        BadgeTone.SUCCESS -> colors.successSurface to colors.success
        BadgeTone.WARNING -> colors.warningSurface to colors.warning
        BadgeTone.ERROR -> colors.errorSurface to colors.error
    }
    Box(
        modifier = modifier
            .clip(RoundedCornerShape(Radius.sm))
            .background(bg)
            .padding(horizontal = Spacing.sm, vertical = Spacing.xxs)
    ) {
        Text(text = text, style = AppType.caption, color = fg)
    }
}

@Composable
fun SectionHeader(
    title: String,
    modifier: Modifier = Modifier,
    actionText: String? = null,
    onActionClick: (() -> Unit)? = null
) {
    val colors = SolGridTheme.colors
    Row(
        modifier = modifier.fillMaxWidth().padding(vertical = Spacing.sm),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(text = title, style = AppType.sectionTitle, color = colors.textPrimary)
        if (actionText != null && onActionClick != null) {
            Text(
                text = actionText,
                style = AppType.bodyStrong,
                color = colors.accent,
                modifier = Modifier.clickableNoRipple(onActionClick)
            )
        }
    }
}

@Composable
fun FilterChip(
    label: String,
    selected: Boolean,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    val colors = SolGridTheme.colors
    val bg = if (selected) colors.accent else Color.Transparent
    val fg = if (selected) colors.onAccent else colors.textSecondary
    val border = if (selected) colors.accent else colors.border
    Box(
        modifier = modifier
            .clip(RoundedCornerShape(Radius.pill))
            .background(bg)
            .border(Sizing.borderThin, border, RoundedCornerShape(Radius.pill))
            .clickableNoRipple(onClick)
            .padding(horizontal = Spacing.lg, vertical = Spacing.sm)
    ) {
        Text(text = label, style = AppType.bodyStrong, color = fg)
    }
}

/** Simple full-width divider using the theme border color. */
@Composable
fun AppDivider(modifier: Modifier = Modifier) {
    val colors = SolGridTheme.colors
    Box(
        modifier
            .fillMaxWidth()
            .height(1.dp)
            .background(colors.border)
    )
}
