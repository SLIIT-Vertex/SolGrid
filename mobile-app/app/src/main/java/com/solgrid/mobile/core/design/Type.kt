package com.solgrid.mobile.core.design

import androidx.compose.material3.Typography
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.Font
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.sp

// Uses the system default (Roboto-family) font on device; a custom font can be dropped into
// res/font and swapped in here without touching call sites.
val AppFontFamily = FontFamily.Default

/**
 * SolGrid type scale. Named semantically (ScreenTitle, SectionTitle, Body, Supporting, Caption,
 * ButtonLabel) so screens reach for meaning, not raw sizes.
 */
object AppType {
    val screenTitle = TextStyle(
        fontFamily = AppFontFamily,
        fontWeight = FontWeight.SemiBold,
        fontSize = 26.sp,
        lineHeight = 32.sp,
        letterSpacing = (-0.3).sp
    )
    val sectionTitle = TextStyle(
        fontFamily = AppFontFamily,
        fontWeight = FontWeight.SemiBold,
        fontSize = 17.sp,
        lineHeight = 22.sp,
        letterSpacing = (-0.1).sp
    )
    val body = TextStyle(
        fontFamily = AppFontFamily,
        fontWeight = FontWeight.Normal,
        fontSize = 15.sp,
        lineHeight = 21.sp
    )
    val bodyStrong = TextStyle(
        fontFamily = AppFontFamily,
        fontWeight = FontWeight.Medium,
        fontSize = 15.sp,
        lineHeight = 21.sp
    )
    val supporting = TextStyle(
        fontFamily = AppFontFamily,
        fontWeight = FontWeight.Normal,
        fontSize = 13.sp,
        lineHeight = 18.sp
    )
    val caption = TextStyle(
        fontFamily = AppFontFamily,
        fontWeight = FontWeight.Medium,
        fontSize = 11.sp,
        lineHeight = 14.sp,
        letterSpacing = 0.2.sp
    )
    val buttonLabel = TextStyle(
        fontFamily = AppFontFamily,
        fontWeight = FontWeight.SemiBold,
        fontSize = 15.sp,
        lineHeight = 20.sp
    )
}

val AppTypography = Typography(
    headlineSmall = AppType.screenTitle,
    titleMedium = AppType.sectionTitle,
    bodyLarge = AppType.body,
    bodyMedium = AppType.body,
    labelLarge = AppType.buttonLabel,
    bodySmall = AppType.supporting,
    labelSmall = AppType.caption
)
