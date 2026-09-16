package com.solgrid.mobile.core.design

import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.graphics.Color

/**
 * Extended semantic tokens not modeled by Material3's ColorScheme (surfaceAlt, success/warning
 * surfaces, borders). Access via [LocalAppColors.current] or the [SolGridTheme.colors] helper.
 */
data class AppColors(
    val background: Color,
    val surface: Color,
    val surfaceAlt: Color,
    val textPrimary: Color,
    val textSecondary: Color,
    val textTertiary: Color,
    val border: Color,
    val borderStrong: Color,
    val accent: Color,
    val accentSurface: Color,
    val onAccent: Color,
    val success: Color,
    val successSurface: Color,
    val warning: Color,
    val warningSurface: Color,
    val error: Color,
    val errorSurface: Color
)

val LightAppColors = AppColors(
    background = LightBackground,
    surface = LightSurface,
    surfaceAlt = LightSurfaceAlt,
    textPrimary = LightTextPrimary,
    textSecondary = LightTextSecondary,
    textTertiary = LightTextTertiary,
    border = LightBorder,
    borderStrong = LightBorderStrong,
    accent = Accent,
    accentSurface = AccentSurfaceLight,
    onAccent = OnAccent,
    success = Success,
    successSurface = SuccessSurfaceLight,
    warning = Warning,
    warningSurface = WarningSurfaceLight,
    error = Error,
    errorSurface = ErrorSurfaceLight
)

val DarkAppColors = AppColors(
    background = DarkBackground,
    surface = DarkSurface,
    surfaceAlt = DarkSurfaceAlt,
    textPrimary = DarkTextPrimary,
    textSecondary = DarkTextSecondary,
    textTertiary = DarkTextTertiary,
    border = DarkBorder,
    borderStrong = DarkBorderStrong,
    accent = AccentDark,
    accentSurface = AccentSurfaceDark,
    onAccent = Color.White,
    success = Success,
    successSurface = SuccessSurfaceDark,
    warning = Warning,
    warningSurface = WarningSurfaceDark,
    error = Error,
    errorSurface = ErrorSurfaceDark
)

val LocalAppColors = staticCompositionLocalOf { LightAppColors }
