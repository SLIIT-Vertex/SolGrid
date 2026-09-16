package com.solgrid.mobile.core.components

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.Sizing
import com.solgrid.mobile.core.design.SolGridTheme

@Composable
fun GoogleOAuthButton(
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    loading: Boolean = false,
    label: String = "Continue with Google"
) {
    val colors = SolGridTheme.colors
    OutlinedButton(
        onClick = onClick,
        modifier = modifier.fillMaxWidth().height(Sizing.touchTarget),
        enabled = !loading,
        shape = RoundedCornerShape(Radius.md),
        border = BorderStroke(Sizing.borderThin, colors.border)
    ) {
        Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(10.dp)) {
            if (loading) {
                CircularProgressIndicator(modifier = Modifier.height(18.dp), strokeWidth = 2.dp, color = colors.accent)
                Text("Connecting to Google...", style = AppType.buttonLabel, color = colors.textPrimary)
            } else {
                GoogleMark()
                Text(label, style = AppType.buttonLabel, color = colors.textPrimary)
            }
        }
    }
}
