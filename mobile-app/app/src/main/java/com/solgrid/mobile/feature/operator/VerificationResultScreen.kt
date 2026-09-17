package com.solgrid.mobile.feature.operator

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
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
import androidx.compose.runtime.remember
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
import com.solgrid.mobile.feature.microgrid.NodeViewModel
import com.solgrid.mobile.feature.reservations.statusLabel

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
    val reservation = state.lastVerification?.reservation

    // Pull full station + slot details so the operator sees everything at handover.
    val nodeViewModel = remember { NodeViewModel() }
    val nodeState by nodeViewModel.state.collectAsState()
    LaunchedEffect(code) { viewModel.verifyCode(code) }
    LaunchedEffect(reservation?.nodeId) { reservation?.nodeId?.let { nodeViewModel.loadDetail(it) } }
    val node = nodeState.selected
    val slot = remember(nodeState.slots, reservation?.bookingSlotId) {
        nodeState.slots.find { it.id == reservation?.bookingSlotId }
    }
    // Finalization is only allowed during the reserved window. null = window not yet known (slot
    // still loading); -1 = before window; 0 = within window; 1 = after window.
    val windowPosition: Int? = remember(slot) {
        val s = slot ?: return@remember null
        val now = java.time.Instant.now()
        val start = runCatching { java.time.OffsetDateTime.parse(s.startTime).toInstant() }.getOrNull()
        val end = runCatching { java.time.OffsetDateTime.parse(s.endTime).toInstant() }.getOrNull()
        if (start == null || end == null) null
        else when {
            now.isBefore(start) -> -1
            !now.isBefore(end) -> 1
            else -> 0
        }
    }

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        Column(
            modifier = Modifier
                .weight(1f)
                .verticalScroll(rememberScrollState())
                .padding(horizontal = Spacing.xxl),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Box(modifier = Modifier.padding(top = Spacing.xxl))

            if (state.verifying) {
                Text("Verifying with server…", style = AppType.body, color = colors.textSecondary, modifier = Modifier.padding(top = Spacing.xxl))
            } else {
                val (icon, tone, title, description) = when (state.lastVerification?.result) {
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

                reservation?.let { r ->
                    DetailSection(title = "Prosumer") {
                        r.prosumerName?.takeIf { it.isNotBlank() }?.let { InfoRow("Name", it) }
                        InfoRow("NIC", r.prosumerNic)
                    }
                    DetailSection(title = "Booking") {
                        InfoRow("Reference", r.reference)
                        InfoRow("Date", r.date)
                        InfoRow("Time", "${r.startTime} – ${r.endTime}")
                        InfoRow("Status", statusLabel(r.status))
                    }
                    DetailSection(title = "Station & slot") {
                        InfoRow("Station", node?.name ?: r.nodeName)
                        node?.code?.takeIf { it.isNotBlank() }?.let { InfoRow("Code", it) }
                        node?.addressLine?.takeIf { it.isNotBlank() }?.let { InfoRow("Address", it) }
                        node?.let { InfoRow("Capacity", "${it.capacityKw} kW") }
                        slot?.let { InfoRow("Battery slot", "Slot ${it.slotNumber} · ${it.batteryCapacityKwh.toInt()} kWh") }
                    }
                }
            }

            Box(modifier = Modifier.padding(bottom = Spacing.lg))
        }

        state.operationError?.let { Text(it, style = AppType.body, color = colors.error, textAlign = TextAlign.Center, modifier = Modifier.fillMaxWidth().padding(horizontal = Spacing.xxl, vertical = Spacing.sm)) }

        Column(modifier = Modifier.padding(horizontal = Spacing.xxl)) {
            if (state.lastVerification?.result == TransferVerificationResult.VALID && reservation != null) {
                val outsideWindow = windowPosition == -1 || windowPosition == 1
                if (outsideWindow) {
                    val note = if (windowPosition == -1) {
                        "This can only be finalized during the reserved window (${reservation.date}, ${reservation.startTime} – ${reservation.endTime}). The window hasn't started yet."
                    } else {
                        "This can only be finalized during the reserved window (${reservation.date}, ${reservation.startTime} – ${reservation.endTime}). That window has already passed."
                    }
                    Text(
                        note,
                        style = AppType.supporting,
                        color = colors.warning,
                        textAlign = TextAlign.Center,
                        modifier = Modifier.fillMaxWidth().padding(bottom = Spacing.sm)
                    )
                }
                PrimaryButton(
                    text = "Finalize Energy Transfer",
                    loading = state.finalizing,
                    enabled = !outsideWindow && !state.finalizing,
                    onClick = { viewModel.finalizeTransfer(onDone) },
                    modifier = Modifier.fillMaxWidth()
                )
            }
            SecondaryButton(text = "Scan Another", onClick = onScanAnother, modifier = Modifier.fillMaxWidth().padding(top = Spacing.md, bottom = Spacing.xl))
        }
    }
}

@Composable
private fun DetailSection(title: String, content: @Composable () -> Unit) {
    val colors = SolGridTheme.colors
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(top = Spacing.lg)
            .clip(RoundedCornerShape(Radius.lg))
            .background(colors.surface)
            .padding(Spacing.lg),
        verticalArrangement = Arrangement.spacedBy(Spacing.xxs)
    ) {
        Text(title, style = AppType.caption, color = colors.textSecondary, modifier = Modifier.padding(bottom = Spacing.xs))
        content()
    }
}

@Composable
private fun InfoRow(label: String, value: String) {
    val colors = SolGridTheme.colors
    Row(
        modifier = Modifier.fillMaxWidth().padding(vertical = Spacing.xs),
        horizontalArrangement = androidx.compose.foundation.layout.Arrangement.spacedBy(Spacing.md)
    ) {
        Text(label, style = AppType.supporting, color = colors.textSecondary, modifier = Modifier.weight(1f))
        Text(
            value,
            style = AppType.bodyStrong,
            color = colors.textPrimary,
            textAlign = TextAlign.End,
            modifier = Modifier.weight(1f)
        )
    }
}

private data class Quad(
    val icon: androidx.compose.ui.graphics.vector.ImageVector,
    val tone: Color,
    val title: String,
    val description: String
)
