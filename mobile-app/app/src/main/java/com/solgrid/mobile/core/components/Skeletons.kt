package com.solgrid.mobile.core.components

import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing

/** A single shimmering skeleton block. Building block for screen-specific skeleton layouts. */
@Composable
fun SkeletonBlock(
    modifier: Modifier = Modifier,
    shape: androidx.compose.ui.graphics.Shape = RoundedCornerShape(Radius.sm)
) {
    val colors = SolGridTheme.colors
    val transition = rememberInfiniteTransition(label = "skeleton")
    val alpha by transition.animateFloat(
        initialValue = 0.5f,
        targetValue = 1f,
        animationSpec = infiniteRepeatable(
            animation = tween(700, easing = LinearEasing),
            repeatMode = RepeatMode.Reverse
        ),
        label = "skeletonAlpha"
    )
    androidx.compose.foundation.layout.Box(
        modifier = modifier
            .clip(shape)
            .background(colors.surfaceAlt.copy(alpha = alpha))
    )
}

@Composable
fun ListRowSkeleton(modifier: Modifier = Modifier) {
    Row(
        modifier = modifier.fillMaxWidth().height(56.dp),
        horizontalArrangement = Arrangement.spacedBy(Spacing.md)
    ) {
        SkeletonBlock(Modifier.size(40.dp), shape = CircleShape)
        Column(
            modifier = Modifier.weight(1f),
            verticalArrangement = Arrangement.spacedBy(Spacing.sm)
        ) {
            SkeletonBlock(Modifier.fillMaxWidth(0.6f).height(14.dp))
            SkeletonBlock(Modifier.fillMaxWidth(0.4f).height(12.dp))
        }
    }
}

@Composable
fun HomeSkeleton(modifier: Modifier = Modifier) {
    Column(modifier = modifier.fillMaxWidth(), verticalArrangement = Arrangement.spacedBy(Spacing.lg)) {
        Row(horizontalArrangement = Arrangement.spacedBy(Spacing.md)) {
            SkeletonBlock(Modifier.size(48.dp), shape = CircleShape)
            Column(verticalArrangement = Arrangement.spacedBy(Spacing.sm)) {
                SkeletonBlock(Modifier.width(140.dp).height(14.dp))
                SkeletonBlock(Modifier.width(90.dp).height(12.dp))
            }
        }
        SkeletonBlock(Modifier.fillMaxWidth().height(48.dp))
        Row(horizontalArrangement = Arrangement.spacedBy(Spacing.lg)) {
            repeat(4) { SkeletonBlock(Modifier.size(56.dp), shape = CircleShape) }
        }
        repeat(4) { ListRowSkeleton() }
    }
}

@Composable
fun ListSkeleton(modifier: Modifier = Modifier, count: Int = 6) {
    Column(modifier = modifier.fillMaxWidth(), verticalArrangement = Arrangement.spacedBy(Spacing.md)) {
        repeat(count) { ListRowSkeleton() }
    }
}

@Composable
fun DetailsSkeleton(modifier: Modifier = Modifier) {
    Column(modifier = modifier.fillMaxWidth(), verticalArrangement = Arrangement.spacedBy(Spacing.md)) {
        SkeletonBlock(Modifier.fillMaxWidth(0.7f).height(24.dp))
        SkeletonBlock(Modifier.fillMaxWidth(0.3f).height(16.dp))
        SkeletonBlock(Modifier.fillMaxWidth().height(1.dp))
        repeat(3) { SkeletonBlock(Modifier.fillMaxWidth().height(14.dp)) }
        SkeletonBlock(Modifier.fillMaxWidth(0.5f).height(14.dp))
    }
}

@Composable
fun ProfileSkeleton(modifier: Modifier = Modifier) {
    Column(modifier = modifier.fillMaxWidth(), horizontalAlignment = androidx.compose.ui.Alignment.CenterHorizontally) {
        SkeletonBlock(Modifier.size(72.dp), shape = CircleShape)
        SkeletonBlock(Modifier.padding(top = Spacing.md).width(120.dp).height(16.dp))
        SkeletonBlock(Modifier.padding(top = Spacing.sm).width(160.dp).height(12.dp))
    }
}
