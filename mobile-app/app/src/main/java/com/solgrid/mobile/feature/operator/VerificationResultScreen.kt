package com.solgrid.mobile.feature.operator

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.CheckCircle
import androidx.compose.material.icons.outlined.ErrorOutline
import androidx.compose.material.icons.outlined.EventBusy
import androidx.compose.material.icons.outlined.TaskAlt
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.components.PrimaryButton
import com.solgrid.mobile.core.components.SecondaryButton
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.models.TransferVerificationResult

/** Shows verify result and lets the operator finalize a valid transfer (MOB-12). */
@Composable
fun VerificationResultScreen(
    viewModel: OperatorViewModel,
    code: String,
    onDone: () -> Unit,
    onScanAnother: () -> Unit
) {
    val colors = SolGridTheme.colors
    val state by viewModel.uiState.collectAsState()

    LaunchedEffect(code) { viewModel.verifyCode(code) }

    Column(
        modifier = Modifier.fillMaxSize().background(colors.background).padding(Spacing.xxl),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Box(modifier = Modifier.weight(1f))

        if (state.verifying) {
            Text("Verifying with server...", style = AppType.body, color = colors.textSecondary)
        } else {
            val verification = state.lastVerification
            val (icon, tone, title, description) = when (verification?.result) {
                TransferVerificationResult.VALID -> Quad(Icons.Outlined.CheckCircle, colors.success, "Reservation Verified", "This booking is approved and ready for transfer.")
                TransferVerificationResult.ALREADY_COMPLETED -> Quad(Icons.Outlined.TaskAlt, colors.textSecondary, "Already Completed", "This energy transfer was already finalized.")
                TransferVerificationResult.EXPIRED -> Quad(Icons.Outlined.EventBusy, colors.warning, "Not Ready", "This reservation is not currently approved for transfer.")
                else -> Quad(Icons.Outlined.ErrorOutline, colors.error, "Code Not Recognized", "The server could not match this code to a reservation.")
            }

            Box(
                modifier = Modifier.size(72.dp).clip(CircleShape).background(tone.copy(alpha = 0.12f)),
                contentAlignment = Alignment.Center
            ) {
                Icon(icon, contentDescription = null, tint = tone, modifier = Modifier.size(36.dp))
            }
            Text(title, style = AppType.screenTitle, color = colors.textPrimary, textAlign = TextAlign.Center, modifier = Modifier.padding(top = Spacing.xl))
            Text(description, style = AppType.body, color = colors.textSecondary, textAlign = TextAlign.Center, modifier = Modifier.padding(top = Spacing.sm))

            verification?.reservation?.let { reservation ->
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(top = Spacing.xl)
                        .clip(RoundedCornerShape(Radius.lg))
                        .background(colors.surface)
                        .padding(Spacing.lg)
                ) {
                    InfoRow("Reservation ID", reservation.id)
                    InfoRow("Prosumer NIC", reservation.prosumerNic)
                    InfoRow("Node", reservation.nodeName)
                    InfoRow("Slot", "${reservation.date} · ${reservation.startTime}-${reservation.endTime}")
                    InfoRow("Energy", "${reservation.energyKwh} kWh")
                }
            }
        }

        Box(modifier = Modifier.weight(1f))

        if (state.lastVerification?.result == TransferVerificationResult.VALID) {
            PrimaryButton(
                text = "Finalize Energy Transfer",
                loading = state.finalizing,
                onClick = { viewModel.finalizeTransfer(onDone) },
                modifier = Modifier.fillMaxWidth()
            )
        }
        SecondaryButton(text = "Scan Another", onClick = onScanAnother, modifier = Modifier.fillMaxWidth().padding(top = Spacing.md, bottom = Spacing.xl))
    }
}

@Composable
private fun InfoRow(label: String, value: String) {
    val colors = SolGridTheme.colors
    Row(modifier = Modifier.fillMaxWidth().padding(vertical = Spacing.xs), horizontalArrangement = androidx.compose.foundation.layout.Arrangement.SpaceBetween) {
        Text(label, style = AppType.supporting, color = colors.textSecondary)
        Text(value, style = AppType.bodyStrong, color = colors.textPrimary)
    }
}

private data class Quad(
    val icon: androidx.compose.ui.graphics.vector.ImageVector,
    val tone: Color,
    val title: String,
    val description: String
)
