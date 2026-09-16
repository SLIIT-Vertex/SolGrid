package com.solgrid.mobile.feature.prosumer

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
fun ReservationQrScreen(viewModel: ProsumerViewModel, reservationId: String, onBack: () -> Unit) {
    val colors = SolGridTheme.colors
    val reservation = viewModel.reservationById(reservationId)
    var qrPayload by remember { mutableStateOf<String?>(null) }
    var qrError by remember { mutableStateOf<String?>(null) }
    var loading by remember { mutableStateOf(true) }

    LaunchedEffect(reservationId) {
        if (reservation?.status == ReservationStatus.APPROVED) {
            viewModel.issueReservationQr(
                reservationId,
                onError = { loading = false; qrError = it },
            ) { payload, _ ->
                loading = false
                qrPayload = payload
            }
        } else {
            loading = false
        }
    }

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Transaction QR", onBack = onBack)

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
                modifier = Modifier.padding(top = Spacing.xxxl)
            )
            return@Column
        }

        Column(
            modifier = Modifier.fillMaxSize().padding(Spacing.xxl),
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

            QrCodeVisual(payload = payload)

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
                InfoRow(label = "Reservation ID", value = reservation.id)
                InfoRow(label = "Station", value = reservation.nodeName)
                InfoRow(label = "Time", value = reservation.startTime)
            }

            Text(
                "This code is unique to your reservation and expires once the transfer is finalized.",
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
        Text(value, style = AppType.bodyStrong, color = colors.textPrimary)
    }
}
