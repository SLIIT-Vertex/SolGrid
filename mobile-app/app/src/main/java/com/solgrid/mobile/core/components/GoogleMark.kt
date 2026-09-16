package com.solgrid.mobile.core.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.size
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.unit.dp

/**
 * Minimal custom "G" mark used to represent Google in OAuth UI, since Material Icons does not
 * ship a brand glyph. Renders four quadrant arcs approximating the multicolor Google "G".
 */
@Composable
fun GoogleMark(modifier: Modifier = Modifier, size: androidx.compose.ui.unit.Dp = 20.dp) {
    Canvas(modifier = modifier.size(size)) {
        val strokeWidth = this.size.minDimension * 0.22f
        val radius = (this.size.minDimension - strokeWidth) / 2f
        val center = Offset(this.size.width / 2f, this.size.height / 2f)
        val arcSize = androidx.compose.ui.geometry.Size(radius * 2, radius * 2)
        val topLeft = Offset(center.x - radius, center.y - radius)

        drawArc(Color(0xFF4285F4), startAngle = -50f, sweepAngle = 95f, useCenter = false, topLeft = topLeft, size = arcSize, style = Stroke(strokeWidth))
        drawArc(Color(0xFF34A853), startAngle = 45f, sweepAngle = 85f, useCenter = false, topLeft = topLeft, size = arcSize, style = Stroke(strokeWidth))
        drawArc(Color(0xFFFBBC05), startAngle = 130f, sweepAngle = 80f, useCenter = false, topLeft = topLeft, size = arcSize, style = Stroke(strokeWidth))
        drawArc(Color(0xFFEA4335), startAngle = 210f, sweepAngle = 95f, useCenter = false, topLeft = topLeft, size = arcSize, style = Stroke(strokeWidth))
        // Center bar of the "G"
        drawLine(
            color = Color(0xFF4285F4),
            start = center,
            end = Offset(center.x + radius, center.y),
            strokeWidth = strokeWidth * 0.8f
        )
    }
}
