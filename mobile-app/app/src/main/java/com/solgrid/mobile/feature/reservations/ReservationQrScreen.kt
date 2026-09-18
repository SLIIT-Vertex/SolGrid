package com.solgrid.mobile.feature.reservations
import com.solgrid.mobile.feature.prosumer.ProsumerViewModel

import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.compose.foundation.background
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.ErrorOutline
import androidx.compose.material.icons.outlined.HourglassEmpty
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.style.TextAlign
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.BadgeTone
import com.solgrid.mobile.core.components.QrCodeVisual
import com.solgrid.mobile.core.components.StatusBadge
import com.solgrid.mobile.core.components.StatePlaceholder
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.models.ReservationStatus

/** Secure transaction QR for an approved reservation (MOB-11), presented at the node for the Grid Operator to scan.
 * The QR payload is fetched fresh from the server each visit — issuing a token replaces any
 * previous one, so this screen must not reuse a stale local value. */
@Composable
fun ReservationQrScreen(viewModel: ProsumerViewModel, reservationId: String, onBack: () -> Unit, title: String = "Transaction QR") {
    val colors = SolGridTheme.colors
    val state by viewModel.uiState.collectAsStateWithLifecycle()
    val reservation = state.reservations.find { it.id == reservationId }
    var qrPayload by remember(reservationId) { mutableStateOf<String?>(null) }
    var qrError by remember(reservationId) { mutableStateOf<String?>(null) }
    var loading by remember(reservationId) { mutableStateOf(true) }

    var expiresAt by remember(reservationId) { mutableStateOf<String?>(null) }
    var expired by remember(reservationId) { mutableStateOf(false) }
    fun requestQr() {
        loading = true
        qrError = null
        qrPayload = null
        expiresAt = null
        expired = false
        viewModel.issueReservationQr(reservationId, onError = { loading = false; qrError = it }) { payload, expiry ->
            loading = false
            qrPayload = payload
            expiresAt = expiry
        }
    }
    LaunchedEffect(reservationId, reservation?.status) {
        if (reservation?.status == ReservationStatus.APPROVED) {
            // Reuse the previously issued code (SQLite) while it's still valid instead of re-minting.
            val cached = viewModel.cachedQr(reservationId)
            if (cached != null) { qrPayload = cached.first; expiresAt = cached.second; loading = false } else requestQr()
        } else loading = false
    }
    LaunchedEffect(expiresAt) {
        val expiry = expiresAt ?: return@LaunchedEffect
        val milliseconds = java.time.Duration.between(java.time.Instant.now(), java.time.OffsetDateTime.parse(expiry).toInstant()).toMillis()
        if (milliseconds > 0) kotlinx.coroutines.delay(milliseconds)
        expired = true
    }

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = title, onBack = onBack)

        if (reservation == null || reservation.status != ReservationStatus.APPROVED) {
            StatePlaceholder(
                icon = Icons.Outlined.HourglassEmpty,
                title = "QR not available yet",
                description = "This reservation must be approved by a Grid Operator before a transaction QR is issued.",
                modifier = Modifier.padding(top = Spacing.xxxl)
            )
            return@Column
        }

        if (loading) {
            Text(
                "Requesting transaction code…",
                style = AppType.body,
                color = colors.textSecondary,
                modifier = Modifier.padding(top = Spacing.xxxl, start = Spacing.lg)
            )
            return@Column
        }

        val payload = qrPayload
        if (qrError != null || payload == null) {
            StatePlaceholder(
                icon = Icons.Outlined.ErrorOutline,
                title = "Couldn't generate QR",
                description = qrError ?: "Something went wrong. Please try again.",
                actionText = "Try again",
                onAction = { requestQr() },
                modifier = Modifier.padding(top = Spacing.xxxl)
            )
            return@Column
        }

        Column(
            modifier = Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(Spacing.xxl),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Text(
                "Show this code to the Grid Operator",
                style = AppType.sectionTitle,
                color = colors.textPrimary,
                textAlign = TextAlign.Center,
                modifier = Modifier.padding(top = Spacing.lg)
            )
            Text(
                "${reservation.nodeName} · ${reservation.date}, ${reservation.startTime}",
                style = AppType.supporting,
                color = colors.textSecondary,
                textAlign = TextAlign.Center,
                modifier = Modifier.padding(top = Spacing.xs, bottom = Spacing.xxl)
            )

            if (expired) Text("This QR has expired. Generate a new code before presenting it.", style = AppType.body, color = colors.error, textAlign = TextAlign.Center)
            else QrCodeVisual(payload = payload)
            com.solgrid.mobile.core.components.SecondaryButton("Generate new QR", { requestQr() }, modifier = Modifier.padding(top = Spacing.md))

            Row(modifier = Modifier.padding(top = Spacing.xl)) {
                StatusBadge(text = "Approved", tone = BadgeTone.SUCCESS)
            }

            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(top = Spacing.xxl)
                    .clip(RoundedCornerShape(Radius.lg))
                    .background(colors.surface)
                    .padding(Spacing.lg)
            ) {
                InfoRow(label = "Booking reference", value = reservation.reference)
                InfoRow(label = "Station", value = reservation.nodeName)
                InfoRow(label = "Date", value = reservation.date)
                InfoRow(label = "Time", value = reservation.startTime)
                expiresAt?.let { InfoRow(label = "QR expires", value = java.time.OffsetDateTime.parse(it).atZoneSameInstant(java.time.ZoneId.systemDefault()).format(java.time.format.DateTimeFormatter.ofPattern("MMM d, HH:mm"))) }
            }

            Text(
                "This code expires at the time shown above. Completing the transfer or generating a new code invalidates it.",
                style = AppType.caption,
                color = colors.textTertiary,
                textAlign = TextAlign.Center,
                modifier = Modifier.padding(top = Spacing.lg)
            )
        }
    }
}

@Composable
private fun InfoRow(label: String, value: String) {
    val colors = SolGridTheme.colors
    Row(modifier = Modifier.fillMaxWidth().padding(vertical = Spacing.xs), horizontalArrangement = androidx.compose.foundation.layout.Arrangement.SpaceBetween) {
        Text(label, style = AppType.supporting, color = colors.textSecondary)
        Text(value, style = AppType.bodyStrong, color = colors.textPrimary, textAlign = TextAlign.End, modifier = Modifier.weight(1f).padding(start = Spacing.md))
    }
}
