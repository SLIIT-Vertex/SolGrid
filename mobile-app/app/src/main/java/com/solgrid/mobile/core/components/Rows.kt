package com.solgrid.mobile.core.components

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Circle
import androidx.compose.material.icons.outlined.ChevronRight
import androidx.compose.material3.Icon
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.Switch
import androidx.compose.material3.SwitchDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.Sizing
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing

/** Generic tappable list row: icon in a soft tile, title/subtitle, optional trailing content. */
@Composable
fun ListRow(
    title: String,
    modifier: Modifier = Modifier,
    subtitle: String? = null,
    icon: ImageVector? = null,
    iconTint: androidx.compose.ui.graphics.Color? = null,
    trailing: @Composable (() -> Unit)? = null,
    showChevron: Boolean = false,
    onClick: (() -> Unit)? = null
) {
    val colors = SolGridTheme.colors
    Row(
        modifier = modifier
            .fillMaxWidth()
            .then(if (onClick != null) Modifier.clickable(onClick = onClick) else Modifier)
            .padding(vertical = Spacing.md),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(Spacing.md)
    ) {
        if (icon != null) {
            Box(
                modifier = Modifier
                    .size(Sizing.avatarSm + 4.dp)
                    .clip(RoundedCornerShape(Radius.sm))
                    .background(colors.surfaceAlt),
                contentAlignment = Alignment.Center
            ) {
                Icon(icon, contentDescription = null, tint = iconTint ?: colors.textSecondary, modifier = Modifier.size(Sizing.iconMd))
            }
        }
        Column(modifier = Modifier.weight(1f)) {
            Text(title, style = AppType.bodyStrong, color = colors.textPrimary, maxLines = 1, overflow = TextOverflow.Ellipsis)
            if (subtitle != null) {
                Text(subtitle, style = AppType.supporting, color = colors.textSecondary, maxLines = 1, overflow = TextOverflow.Ellipsis)
            }
        }
        if (trailing != null) {
            trailing()
        } else if (showChevron) {
            Icon(Icons.Outlined.ChevronRight, contentDescription = null, tint = colors.textTertiary)
        }
    }
}

/** Settings screen row: title + optional description, trailing switch/chevron. */
@Composable
fun SettingRow(
    title: String,
    modifier: Modifier = Modifier,
    description: String? = null,
    icon: ImageVector? = null,
    checked: Boolean? = null,
    onCheckedChange: ((Boolean) -> Unit)? = null,
    onClick: (() -> Unit)? = null,
    showChevron: Boolean = onClick != null && checked == null
) {
    ListRow(
        title = title,
        subtitle = description,
        icon = icon,
        modifier = modifier,
        onClick = onClick,
        showChevron = showChevron,
        trailing = if (checked != null) {
            {
                Switch(
                    checked = checked,
                    onCheckedChange = onCheckedChange,
                    colors = SwitchDefaults.colors(checkedTrackColor = SolGridTheme.colors.accent)
                )
            }
        } else null
    )
}

/** Security-specific row with a status badge (Connected / Enabled / etc.) alongside chevron. */
@Composable
fun SecuritySettingRow(
    title: String,
    statusText: String,
    statusTone: BadgeTone,
    modifier: Modifier = Modifier,
    icon: ImageVector? = null,
    onClick: (() -> Unit)? = null
) {
    ListRow(
        title = title,
        icon = icon,
        modifier = modifier,
        onClick = onClick,
        trailing = {
            Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(Spacing.sm)) {
                StatusBadge(statusText, statusTone)
                Icon(Icons.Outlined.ChevronRight, contentDescription = null, tint = SolGridTheme.colors.textTertiary)
            }
        }
    )
}

/** Timeline-style row used for Activity feed and Recent Activity on Home. */
@Composable
fun ActivityRow(
    title: String,
    timestamp: String,
    modifier: Modifier = Modifier,
    subtitle: String? = null,
    icon: ImageVector? = null,
    isAlert: Boolean = false
) {
    val colors = SolGridTheme.colors
    Row(
        modifier = modifier.fillMaxWidth().padding(vertical = Spacing.sm),
        horizontalArrangement = Arrangement.spacedBy(Spacing.md)
    ) {
        Box(
            modifier = Modifier
                .size(Sizing.avatarSm + 4.dp)
                .clip(CircleShape)
                .background(if (isAlert) colors.errorSurface else colors.surfaceAlt),
            contentAlignment = Alignment.Center
        ) {
            if (icon != null) {
                Icon(
                    icon,
                    contentDescription = null,
                    tint = if (isAlert) colors.error else colors.textSecondary,
                    modifier = Modifier.size(Sizing.iconSm)
                )
            }
        }
        Column(modifier = Modifier.weight(1f)) {
            Text(title, style = AppType.bodyStrong, color = colors.textPrimary)
            if (subtitle != null) {
                Text(subtitle, style = AppType.supporting, color = colors.textSecondary)
            }
        }
        Text(timestamp, style = AppType.caption, color = colors.textTertiary)
    }
}

/** Notification center row with unread dot indicator. */
@Composable
fun NotificationRow(
    title: String,
    description: String,
    timestamp: String,
    icon: ImageVector,
    isRead: Boolean,
    modifier: Modifier = Modifier,
    onClick: () -> Unit = {}
) {
    val colors = SolGridTheme.colors
    Row(
        modifier = modifier
            .fillMaxWidth()
            .clickable(onClick = onClick)
            .padding(vertical = Spacing.md),
        horizontalArrangement = Arrangement.spacedBy(Spacing.md)
    ) {
        Box(
            modifier = Modifier
                .size(Sizing.avatarSm + 4.dp)
                .clip(RoundedCornerShape(Radius.sm))
                .background(colors.surfaceAlt),
            contentAlignment = Alignment.Center
        ) {
            Icon(icon, contentDescription = null, tint = colors.textSecondary, modifier = Modifier.size(Sizing.iconMd))
        }
        Column(modifier = Modifier.weight(1f)) {
            Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(Spacing.xs)) {
                Text(
                    title,
                    style = if (isRead) AppType.bodyStrong else AppType.bodyStrong,
                    color = colors.textPrimary,
                    modifier = Modifier.weight(1f, fill = false)
                )
                if (!isRead) {
                    Icon(Icons.Filled.Circle, contentDescription = "Unread", tint = colors.accent, modifier = Modifier.size(7.dp))
                }
            }
            Text(description, style = AppType.supporting, color = colors.textSecondary, maxLines = 2, overflow = TextOverflow.Ellipsis)
            Text(timestamp, style = AppType.caption, color = colors.textTertiary, modifier = Modifier.padding(top = Spacing.xxs))
        }
    }
}

/** Row representing a file in an upload list, with progress + status. */
@Composable
fun FileRow(
    fileName: String,
    fileType: String,
    sizeLabel: String,
    progress: Float,
    statusLabel: String,
    statusTone: BadgeTone,
    modifier: Modifier = Modifier,
    icon: ImageVector,
    onRemove: (() -> Unit)? = null
) {
    val colors = SolGridTheme.colors
    Row(
        modifier = modifier.fillMaxWidth().padding(vertical = Spacing.sm),
        horizontalArrangement = Arrangement.spacedBy(Spacing.md),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Box(
            modifier = Modifier
                .size(Sizing.avatarSm + 4.dp)
                .clip(RoundedCornerShape(Radius.sm))
                .background(colors.surfaceAlt),
            contentAlignment = Alignment.Center
        ) {
            Icon(icon, contentDescription = null, tint = colors.textSecondary, modifier = Modifier.size(Sizing.iconMd))
        }
        Column(modifier = Modifier.weight(1f)) {
            Text(fileName, style = AppType.bodyStrong, color = colors.textPrimary, maxLines = 1, overflow = TextOverflow.Ellipsis)
            Text("$fileType • $sizeLabel", style = AppType.supporting, color = colors.textSecondary)
            if (progress in 0f..0.999f) {
                LinearProgressIndicator(
                    progress = { progress },
                    modifier = Modifier.fillMaxWidth().padding(top = Spacing.xs),
                    color = colors.accent,
                    trackColor = colors.surfaceAlt
                )
            }
        }
        StatusBadge(statusLabel, statusTone)
    }
}
