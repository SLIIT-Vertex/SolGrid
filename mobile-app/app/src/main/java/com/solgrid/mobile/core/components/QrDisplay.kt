package com.solgrid.mobile.core.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import kotlin.random.Random

/**
 * Deterministic pseudo-QR pattern rendered from a payload string. Not a real scannable code —
 * this app has no camera/QR SDK wired up — but visually represents "a QR is here" for the
 * transaction QR and operator-scan mock flows. A real implementation would use ZXing to both
 * generate this bitmap and decode camera frames.
 */
@Composable
fun QrCodeVisual(payload: String, modifier: Modifier = Modifier, size: Dp = 220.dp) {
    val colors = SolGridTheme.colors
    val seed = payload.hashCode()
    Canvas(
        modifier = modifier
            .size(size)
            .clip(RoundedCornerShape(Radius.md))
            .background(Color.White)
    ) {
        val gridCount = 21
        val cell = this.size.minDimension / gridCount
        val random = Random(seed)
        // Finder squares (corners) drawn first for a QR-like silhouette.
        val finderSize = cell * 5
        listOf(
            Offset(0f, 0f),
            Offset(this.size.width - finderSize, 0f),
            Offset(0f, this.size.height - finderSize)
        ).forEach { corner ->
            drawRect(Color.Black, topLeft = corner, size = Size(finderSize, finderSize))
            drawRect(Color.White, topLeft = corner + Offset(cell, cell), size = Size(finderSize - cell * 2, finderSize - cell * 2))
            drawRect(Color.Black, topLeft = corner + Offset(cell * 2, cell * 2), size = Size(finderSize - cell * 4, finderSize - cell * 4))
        }
        for (row in 0 until gridCount) {
            for (col in 0 until gridCount) {
                val inFinderZone =
                    (row < 7 && col < 7) || (row < 7 && col >= gridCount - 7) || (row >= gridCount - 7 && col < 7)
                if (inFinderZone) continue
                if (random.nextBoolean()) {
                    drawRect(
                        Color.Black,
                        topLeft = Offset(col * cell, row * cell),
                        size = Size(cell * 0.92f, cell * 0.92f)
                    )
                }
            }
        }
    }
}
