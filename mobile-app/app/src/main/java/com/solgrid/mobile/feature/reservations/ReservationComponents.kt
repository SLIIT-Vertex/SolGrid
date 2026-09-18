package com.solgrid.mobile.feature.reservations

import androidx.compose.animation.animateColorAsState
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Bolt
import androidx.compose.material.icons.outlined.Check
import androidx.compose.material.icons.outlined.ChevronRight
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.components.BadgeTone
import com.solgrid.mobile.core.components.StatusBadge
import com.solgrid.mobile.core.models.ReservationStatus
import com.solgrid.mobile.core.components.clickableNoRipple
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.Sizing
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.models.EnergyReservation
import com.solgrid.mobile.core.network.BookingSlotDto
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter

private val slotDayFormatter = DateTimeFormatter.ofPattern("EEE, MMM d")
private val slotTimeFormatter = DateTimeFormatter.ofPattern("hh:mm a")

/** Human-friendly two-line label for a booking slot, e.g. "Wed, Sep 24" / "09:00 AM – 11:00 AM". */
fun slotDayLabel(slot: BookingSlotDto): String = runCatching {
    OffsetDateTime.parse(slot.startTime).atZoneSameInstant(ZoneId.systemDefault()).format(slotDayFormatter)
}.getOrDefault(slot.startTime)

fun slotTimeLabel(slot: BookingSlotDto): String = runCatching {
    val start = OffsetDateTime.parse(slot.startTime).atZoneSameInstant(ZoneId.systemDefault())
    val end = OffsetDateTime.parse(slot.endTime).atZoneSameInstant(ZoneId.systemDefault())
    "${start.format(slotTimeFormatter)} – ${end.format(slotTimeFormatter)}"
}.getOrDefault("${slot.startTime} – ${slot.endTime}")

/**
 * A tappable slot card used on the Create/Reschedule screens. Selecting it draws an accent ring
 * and a check; unavailable (reserved/occupied) slots are dimmed and show a status pill instead.
 */
@Composable
fun SlotSelectCard(
    slot: BookingSlotDto,
    selected: Boolean,
    enabled: Boolean,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    val colors = SolGridTheme.colors
    val borderColor by animateColorAsState(
        targetValue = if (selected) colors.accent else colors.border,
        label = "slotBorder"
    )
    val background = when {
        selected -> colors.accentSurface
        !enabled -> colors.surfaceAlt
        else -> colors.surface
    }
    Row(
        modifier = modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(Radius.lg))
            .background(background)
            .border(if (selected) 1.5.dp else Sizing.borderThin, borderColor, RoundedCornerShape(Radius.lg))
            .then(if (enabled) Modifier.clickableNoRipple(onClick) else Modifier)
            .padding(Spacing.md),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(Spacing.md)
    ) {
        Box(
            modifier = Modifier
                .size(44.dp)
                .clip(RoundedCornerShape(Radius.md))
                .background(if (selected) colors.accent else colors.accentSurface),
            contentAlignment = Alignment.Center
        ) {
            Icon(
                Icons.Outlined.Bolt,
                contentDescription = null,
                tint = if (selected) colors.onAccent else colors.accent,
                modifier = Modifier.size(22.dp)
            )
        }
        Column(modifier = Modifier.weight(1f)) {
            Text(
                slotDayLabel(slot),
                style = AppType.bodyStrong,
                color = if (enabled) colors.textPrimary else colors.textSecondary
            )
            Text(
                slotTimeLabel(slot),
                style = AppType.supporting,
                color = colors.textSecondary,
                modifier = Modifier.padding(top = Spacing.xxs)
            )
            Text(
                "Slot ${slot.slotNumber} · ${slot.batteryCapacityKwh.toInt()} kWh battery",
                style = AppType.caption,
                color = colors.textTertiary,
                modifier = Modifier.padding(top = Spacing.xxs)
            )
        }
        if (!enabled) {
            StatusBadge(text = "Reserved", tone = BadgeTone.NEUTRAL)
        } else {
            Box(
                modifier = Modifier
                    .size(24.dp)
                    .clip(CircleShape)
                    .background(if (selected) colors.accent else colors.surfaceAlt),
                contentAlignment = Alignment.Center
            ) {
                if (selected) {
                    Icon(Icons.Outlined.Check, contentDescription = "Selected", tint = colors.onAccent, modifier = Modifier.size(16.dp))
                }
            }
        }
    }
}

/** Rich, tappable booking card for the Bookings & History lists. */
@Composable
fun BookingListCard(reservation: EnergyReservation, onClick: () -> Unit, modifier: Modifier = Modifier) {
    val colors = SolGridTheme.colors
    Row(
        modifier = modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(Radius.lg))
            .background(colors.surface)
            .border(Sizing.borderThin, colors.border, RoundedCornerShape(Radius.lg))
            .clickableNoRipple(onClick)
            .padding(Spacing.md),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(Spacing.md)
    ) {
        Box(
            modifier = Modifier.size(46.dp).clip(RoundedCornerShape(Radius.md)).background(colors.accentSurface),
            contentAlignment = Alignment.Center
        ) {
            Icon(Icons.Outlined.Bolt, contentDescription = null, tint = colors.accent)
        }
        Column(modifier = Modifier.weight(1f)) {
            Text(reservation.nodeName, style = AppType.bodyStrong, color = colors.textPrimary)
            Text(
                "${reservation.date} · ${reservation.startTime} – ${reservation.endTime}",
                style = AppType.supporting,
                color = colors.textSecondary,
                modifier = Modifier.padding(top = Spacing.xxs)
            )
            StatusBadge(
                text = statusLabel(reservation.status),
                tone = statusTone(reservation.status),
                modifier = Modifier.padding(top = Spacing.sm)
            )
        }
        Icon(Icons.Outlined.ChevronRight, contentDescription = null, tint = colors.textTertiary)
    }
}

/** Section wrapper: a titled surface card that groups related detail rows. */
@Composable
fun DetailCard(
    title: String,
    modifier: Modifier = Modifier,
    content: @Composable () -> Unit
) {
    val colors = SolGridTheme.colors
    Column(modifier = modifier.fillMaxWidth()) {
        Text(
            title.uppercase(),
            style = AppType.caption,
            color = colors.textTertiary,
            modifier = Modifier.padding(start = Spacing.xs, bottom = Spacing.sm)
        )
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .clip(RoundedCornerShape(Radius.lg))
                .background(colors.surface)
                .border(Sizing.borderThin, colors.border, RoundedCornerShape(Radius.lg))
                .padding(horizontal = Spacing.lg, vertical = Spacing.xs)
        ) {
            content()
        }
    }
}

/** Label/value row for detail cards, with an optional leading icon. */
@Composable
fun DetailRow(
    label: String,
    value: String,
    modifier: Modifier = Modifier,
    icon: ImageVector? = null,
    valueColor: androidx.compose.ui.graphics.Color? = null
) {
    val colors = SolGridTheme.colors
    Row(
        modifier = modifier.fillMaxWidth().padding(vertical = Spacing.md),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(Spacing.md)
    ) {
        if (icon != null) {
            Icon(icon, contentDescription = null, tint = colors.textSecondary, modifier = Modifier.size(Sizing.iconMd))
        }
        Text(label, style = AppType.body, color = colors.textSecondary)
        Text(
            value,
            style = AppType.bodyStrong,
            color = valueColor ?: colors.textPrimary,
            textAlign = TextAlign.End,
            modifier = Modifier.weight(1f)
        )
    }
}

/** Human-readable label for a reservation status. Shared across reservation and dashboard UI. */
fun statusLabel(status: ReservationStatus): String = when (status) {
    ReservationStatus.PENDING -> "Pending"
    ReservationStatus.APPROVED -> "Approved"
    ReservationStatus.REJECTED -> "Rejected"
    ReservationStatus.CANCELLED -> "Cancelled"
    ReservationStatus.COMPLETED -> "Completed"
}

/** Badge tone mapping for a reservation status. Shared across reservation and dashboard UI. */
fun statusTone(status: ReservationStatus): BadgeTone = when (status) {
    ReservationStatus.PENDING -> BadgeTone.WARNING
    ReservationStatus.APPROVED -> BadgeTone.SUCCESS
    ReservationStatus.REJECTED -> BadgeTone.ERROR
    ReservationStatus.CANCELLED -> BadgeTone.ERROR
    ReservationStatus.COMPLETED -> BadgeTone.NEUTRAL
}
