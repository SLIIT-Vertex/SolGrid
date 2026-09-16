package com.solgrid.mobile.feature.shared

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.NotificationsActive
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import com.solgrid.mobile.core.components.AppDivider
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.FilterChip
import com.solgrid.mobile.core.components.SectionHeader
import com.solgrid.mobile.core.components.SettingRow
import com.solgrid.mobile.core.design.AppThemeMode
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing

@Composable
fun SettingsScreen(
    onBack: () -> Unit,
    onThemeModeChange: (AppThemeMode) -> Unit
) {
    val colors = SolGridTheme.colors
    var themeMode by remember { mutableStateOf(AppThemeMode.SYSTEM) }
    var pushNotifications by remember { mutableStateOf(true) }
    var bookingReminders by remember { mutableStateOf(true) }

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Settings", onBack = onBack)
        Column(modifier = Modifier.fillMaxSize().padding(horizontal = Spacing.lg)) {
            SectionHeader(title = "Appearance", modifier = Modifier.padding(top = Spacing.md))
            Row(modifier = Modifier.fillMaxWidth().padding(vertical = Spacing.sm), horizontalArrangement = androidx.compose.foundation.layout.Arrangement.spacedBy(Spacing.sm)) {
                FilterChip("System", themeMode == AppThemeMode.SYSTEM, { themeMode = AppThemeMode.SYSTEM; onThemeModeChange(AppThemeMode.SYSTEM) })
                FilterChip("Light", themeMode == AppThemeMode.LIGHT, { themeMode = AppThemeMode.LIGHT; onThemeModeChange(AppThemeMode.LIGHT) })
                FilterChip("Dark", themeMode == AppThemeMode.DARK, { themeMode = AppThemeMode.DARK; onThemeModeChange(AppThemeMode.DARK) })
            }

            SectionHeader(title = "Notifications", modifier = Modifier.padding(top = Spacing.lg))
            SettingRow(title = "Push Notifications", icon = Icons.Outlined.NotificationsActive, checked = pushNotifications, onCheckedChange = { pushNotifications = it })
            AppDivider()
            SettingRow(title = "Booking Reminders", checked = bookingReminders, onCheckedChange = { bookingReminders = it })
        }
    }
}
