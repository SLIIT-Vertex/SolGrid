package com.solgrid.mobile.core.components

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Sizing

@Composable
fun PrimaryButton(
    text: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    enabled: Boolean = true,
    loading: Boolean = false,
    destructive: Boolean = false
) {
    val colors = SolGridTheme.colors
    val bg = if (destructive) colors.error else colors.accent
    androidx.compose.material3.Button(
        onClick = onClick,
        modifier = modifier.height(Sizing.touchTarget),
        enabled = enabled && !loading,
        shape = RoundedCornerShape(Radius.pill),
        colors = ButtonDefaults.buttonColors(
            containerColor = bg,
            contentColor = Color.White,
            disabledContainerColor = bg.copy(alpha = 0.4f),
            disabledContentColor = Color.White.copy(alpha = 0.8f)
        ),
        contentPadding = PaddingValues(horizontal = 20.dp)
    ) {
        if (loading) {
            CircularProgressIndicator(
                modifier = Modifier.size(18.dp),
                color = Color.White,
                strokeWidth = 2.dp
            )
        } else {
            Text(text, style = AppType.buttonLabel)
        }
    }
}

@Composable
fun SecondaryButton(
    text: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    enabled: Boolean = true
) {
    val colors = SolGridTheme.colors
    OutlinedButton(
        onClick = onClick,
        modifier = modifier.height(Sizing.touchTarget),
        enabled = enabled,
        shape = RoundedCornerShape(Radius.pill),
        border = BorderStroke(Sizing.borderThin, colors.border),
        colors = ButtonDefaults.outlinedButtonColors(contentColor = colors.textPrimary),
        contentPadding = PaddingValues(horizontal = 20.dp)
    ) {
        Text(text, style = AppType.buttonLabel)
    }
}

@Composable
fun TextActionButton(
    text: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    color: Color? = null
) {
    val colors = SolGridTheme.colors
    TextButton(onClick = onClick, modifier = modifier) {
        Text(text, style = AppType.buttonLabel, color = color ?: colors.accent)
    }
}
