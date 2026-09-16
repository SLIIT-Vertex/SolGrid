package com.solgrid.mobile.core.components

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxScope
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import com.solgrid.mobile.core.design.SolGridTheme

/** Apply and consume system bars, display cutouts and keyboard insets once for every screen.
 * Based on Android's Compose inset guidance:
 * https://developer.android.com/develop/ui/compose/system/insets-ui
 */
@Composable
fun AppSafeArea(content: @Composable BoxScope.() -> Unit) {
    Box(
        modifier = Modifier.fillMaxSize()
            .background(SolGridTheme.colors.background)
            .safeDrawingPadding(),
        content = content,
    )
}
