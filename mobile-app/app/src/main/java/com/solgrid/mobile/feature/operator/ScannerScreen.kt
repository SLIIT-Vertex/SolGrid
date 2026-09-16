package com.solgrid.mobile.feature.operator

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.CameraAlt
import androidx.compose.material.icons.outlined.QrCodeScanner
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.components.AppTextField
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.PrimaryButton
import com.solgrid.mobile.core.components.SecondaryButton
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.mock.MockData

/**
 * Operator QR scanner (MOB-12). No camera/QR SDK is wired into this frontend-only build, so the
 * viewfinder is a labelled placeholder; "Simulate Scan" and the manual code field stand in for a
 * real camera capture + ZXing/ML Kit decode, which would call the same [onCodeScanned] callback.
 */
@Composable
fun ScannerScreen(onBack: () -> Unit, onCodeScanned: (String) -> Unit) {
    val colors = SolGridTheme.colors
    var manualCode by remember { mutableStateOf("") }

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Scan Prosumer QR", onBack = onBack)
        Column(modifier = Modifier.fillMaxSize().padding(horizontal = Spacing.lg)) {
            Text(
                "Point the camera at the prosumer's transaction QR code to verify their reservation.",
                style = AppType.body,
                color = colors.textSecondary,
                textAlign = TextAlign.Center,
                modifier = Modifier.fillMaxWidth().padding(top = Spacing.lg)
            )

            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .aspectRatio(1f)
                    .padding(top = Spacing.xl)
                    .clip(RoundedCornerShape(Radius.lg))
                    .background(androidx.compose.ui.graphics.Color(0xFF1C1C1C))
                    .border(2.dp, colors.accent, RoundedCornerShape(Radius.lg)),
                contentAlignment = Alignment.Center
            ) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Icon(Icons.Outlined.QrCodeScanner, contentDescription = null, tint = colors.accent.copy(alpha = 0.8f), modifier = Modifier.padding(bottom = Spacing.sm))
                    Text("Camera viewfinder", style = AppType.supporting, color = androidx.compose.ui.graphics.Color.White.copy(alpha = 0.7f))
                }
            }

            PrimaryButton(
                text = "Simulate Scan (Approved Booking)",
                onClick = { onCodeScanned(MockData.reservations.first { it.qrPayload != null }.qrPayload!!) },
                modifier = Modifier.fillMaxWidth().padding(top = Spacing.xl)
            )

            Text("Or enter the code manually", style = AppType.supporting, color = colors.textTertiary, modifier = Modifier.padding(top = Spacing.xl))
            AppTextField(
                value = manualCode,
                onValueChange = { manualCode = it },
                label = "Transaction code",
                modifier = Modifier.padding(top = Spacing.sm)
            )
            SecondaryButton(
                text = "Verify Code",
                onClick = { if (manualCode.isNotBlank()) onCodeScanned(manualCode) },
                modifier = Modifier.fillMaxWidth().padding(top = Spacing.md, bottom = Spacing.xxxl)
            )
        }
    }
}
