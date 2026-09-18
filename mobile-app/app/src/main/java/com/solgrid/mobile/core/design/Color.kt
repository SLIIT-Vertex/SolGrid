package com.solgrid.mobile.core.design

import androidx.compose.ui.graphics.Color

/**
 * SolGrid brand palette matched to the reference UI set: near-black ink for text and the bottom
 * nav, warm gold/amber as the single distinctive accent, light neutral grays for stat tiles, and
 * a soft cream page background.
 */

// Brand
val Navy = Color(0xFF181511) // near-black ink used for primary text + the bottom nav bar
val NavyLight = Color(0xFF2A251E) // lifted ink for elevated dark surfaces
val Copper = Color(0xFF139A68) // brand green accent, matches the web app's brand-600
val CopperLight = Color(0xFF139A68) // same green used consistently on dark backgrounds too
val CopperDeep = Color(0xFF0B6E4B) // darker end of the brand-green gradient on hero cards

// Light theme — soft cream background matching the reference UI set
val LightBackground = Color(0xFFFBF8F2)
val LightSurface = Color(0xFFFFFFFF)
val LightSurfaceAlt = Color(0xFFF3F1EC) // light-gray stat tile fill
val LightTextPrimary = Navy
val LightTextSecondary = Color(0xFF827C71)
val LightTextTertiary = Color(0xFFAFA99B)
val LightBorder = Color(0xFFEDE9E0)
val LightBorderStrong = Color(0xFFDDD6C8)

// Dark theme (ink-tinted neutrals, not pure black)
val DarkBackground = Color(0xFF100D0A)
val DarkSurface = Navy
val DarkSurfaceAlt = NavyLight
val DarkTextPrimary = Color(0xFFF7F4EE)
val DarkTextSecondary = Color(0xFFB4AEA1)
val DarkTextTertiary = Color(0xFF716B5F)
val DarkBorder = Color(0xFF241F19)
val DarkBorderStrong = Color(0xFF352E24)

// Shared accent + semantic colors (consistent across themes for brand recognition)
val Accent = Copper
val AccentDark = CopperLight
val AccentSurfaceLight = Color(0xFFE3F8E3)
val AccentSurfaceDark = Color(0xFF1B3320)

val Success = Color(0xFF16A34A)
val SuccessSurfaceLight = Color(0xFFEAF7EE)
val SuccessSurfaceDark = Color(0xFF122A1B)

val Warning = Color(0xFFD97706)
val WarningSurfaceLight = Color(0xFFFDF3E4)
val WarningSurfaceDark = Color(0xFF2E2110)

val Error = Color(0xFFDC2626)
val ErrorSurfaceLight = Color(0xFFFCEAEA)
val ErrorSurfaceDark = Color(0xFF2E1414)

val OnAccent = Color(0xFFFFFFFF)
