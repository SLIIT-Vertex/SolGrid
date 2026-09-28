package com.solgrid.mobile.feature.reservations

import com.solgrid.mobile.core.network.BookingSlotDto
import java.time.Instant
import org.junit.Assert.assertEquals
import org.junit.Test

class ReservationSlotWindowTest {
    private val now = Instant.parse("2026-09-29T06:00:00Z")

    @Test
    fun keepsOnlyActiveSlotsStartingWithinTheNextSevenDays() {
        val slots = listOf(
            slot("past", "2026-09-29T05:59:59Z"),
            slot("now", "2026-09-29T06:00:00Z"),
            slot("boundary", "2026-10-06T06:00:00Z"),
            slot("too-late", "2026-10-06T06:00:01Z"),
            slot("inactive", "2026-09-30T06:00:00Z", isActive = false),
        )

        assertEquals(
            listOf("now", "boundary"),
            bookingSlotsWithinNextSevenDays(slots, now).map { it.id },
        )
    }

    private fun slot(id: String, startTime: String, isActive: Boolean = true) = BookingSlotDto(
        id = id,
        stationId = "station-1",
        startTime = startTime,
        endTime = startTime,
        slotNumber = 1,
        batteryCapacityKwh = 10.0,
        status = 1,
        isActive = isActive,
        isAvailable = true,
    )
}
