package com.solgrid.mobile.feature.auth

import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.scale
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.R
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import kotlinx.coroutines.delay

/** Premium, minimal splash: logo reveal + short transition, no spinner. */
@Composable
fun SplashScreen(onFinished: () -> Unit) {
    val colors = SolGridTheme.colors
    val scale = remember { Animatable(0.85f) }
    val alpha = remember { Animatable(0f) }

    LaunchedEffect(Unit) {
        alpha.animateTo(1f, tween(400))
        scale.animateTo(1f, tween(500))
        delay(700)
        onFinished()
    }

    Box(
        modifier = Modifier.fillMaxSize().background(colors.background),
        contentAlignment = Alignment.Center
    ) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            modifier = Modifier.graphicsLayer { this.alpha = alpha.value }.scale(scale.value)
        ) {
            Image(
                painter = painterResource(id = R.drawable.app_logo),
                contentDescription = "SolGrid logo",
                modifier = Modifier.size(96.dp)
            )
            Text(
                "SolGrid",
                style = AppType.screenTitle,
                color = colors.textPrimary,
                modifier = Modifier.padding(top = Spacing.lg)
            )
            Text(
                "Smart Solar Microgrid Trading",
                style = AppType.supporting,
                color = colors.textSecondary,
                modifier = Modifier.padding(top = Spacing.xxs)
            )
        }
    }
}
