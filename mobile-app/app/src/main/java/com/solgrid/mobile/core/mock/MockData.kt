package com.solgrid.mobile.core.mock

import com.solgrid.mobile.core.models.BookingSlot
import com.solgrid.mobile.core.models.EnergyReservation
import com.solgrid.mobile.core.models.MicrogridNode
import com.solgrid.mobile.core.models.NodeStatus
import com.solgrid.mobile.core.models.OperatorProfile
import com.solgrid.mobile.core.models.ProsumerAccountStatus
import com.solgrid.mobile.core.models.ProsumerProfile
import com.solgrid.mobile.core.models.ReservationStatus
import com.solgrid.mobile.core.models.SlotStatus

/**
 * Static sample data standing in for the central C# Web API / MongoDB responses. This lets every
 * mobile screen be demonstrated without a live backend — in the real system these values would
 * come from `GET /api/prosumers/{nic}`, `GET /api/nodes`, `GET /api/reservations`, etc.
 */
object MockData {

    val prosumer = ProsumerProfile(
        nic = "200114701234",
        firstName = "Nadeesha",
        lastName = "Perera",
        email = "nadeesha.perera@solgrid.io",
        phone = "0772146630",
        status = ProsumerAccountStatus.ACTIVE
    )

    val operator = OperatorProfile(
        operatorId = "OP-1042",
        fullName = "Ishara Wickramasinghe",
        email = "ishara.w@solgrid.io",
        assignedNodeName = "Colombo Fort Microgrid Hub"
    )

    val nodes = listOf(
        MicrogridNode(
            id = "node-01",
            name = "Colombo Fort Microgrid Hub",
            address = "York Street, Colombo 01",
            latitude = 6.9344,
            longitude = 79.8428,
            capacityKw = 250.0,
            totalSlots = 12,
            availableSlots = 5,
            status = NodeStatus.ACTIVE,
            distanceKm = 1.2
        ),
        MicrogridNode(
            id = "node-02",
            name = "Rajagiriya Substation Node",
            address = "Nawala Road, Rajagiriya",
            latitude = 6.9095,
            longitude = 79.8925,
            capacityKw = 180.0,
            totalSlots = 8,
            availableSlots = 0,
            status = NodeStatus.ACTIVE,
            distanceKm = 3.8
        ),
        MicrogridNode(
            id = "node-03",
            name = "Kelaniya Community Hub",
            address = "Kandy Road, Kelaniya",
            latitude = 6.9553,
            longitude = 79.9219,
            capacityKw = 140.0,
            totalSlots = 10,
            availableSlots = 7,
            status = NodeStatus.ACTIVE,
            distanceKm = 6.5
        ),
        MicrogridNode(
            id = "node-04",
            name = "Maharagama Grid Point",
            address = "High Level Road, Maharagama",
            latitude = 6.8481,
            longitude = 79.9265,
            capacityKw = 200.0,
            totalSlots = 10,
            availableSlots = 4,
            status = NodeStatus.ACTIVE,
            distanceKm = 9.1
        ),
        MicrogridNode(
            id = "node-05",
            name = "Wattala Old Depot Node",
            address = "Negombo Road, Wattala",
            latitude = 6.9890,
            longitude = 79.8929,
            capacityKw = 90.0,
            totalSlots = 6,
            availableSlots = 6,
            status = NodeStatus.INACTIVE,
            distanceKm = 11.4
        )
    )

    fun slotsForNode(nodeId: String): List<BookingSlot> = listOf(
        BookingSlot("$nodeId-s1", nodeId, "Sep 16, 2026", "08:00 AM", "09:00 AM", SlotStatus.AVAILABLE),
        BookingSlot("$nodeId-s2", nodeId, "Sep 16, 2026", "10:00 AM", "11:00 AM", SlotStatus.RESERVED),
        BookingSlot("$nodeId-s3", nodeId, "Sep 16, 2026", "02:00 PM", "03:00 PM", SlotStatus.AVAILABLE),
        BookingSlot("$nodeId-s4", nodeId, "Sep 17, 2026", "09:00 AM", "10:00 AM", SlotStatus.AVAILABLE),
        BookingSlot("$nodeId-s5", nodeId, "Sep 17, 2026", "01:00 PM", "02:00 PM", SlotStatus.UNAVAILABLE)
    )

    val reservations = mutableListOf(
        EnergyReservation(
            id = "res-1001",
            prosumerNic = prosumer.nic,
            nodeId = "node-01",
            nodeName = "Colombo Fort Microgrid Hub",
            date = "Sep 16, 2026",
            startTime = "02:00 PM",
            endTime = "03:00 PM",
            energyKwh = 8.5,
            status = ReservationStatus.APPROVED,
            createdAt = "Sep 14, 2026",
            qrPayload = "SOLGRID|RES-1001|node-01|2026-09-16T14:00"
        ),
        EnergyReservation(
            id = "res-1002",
            prosumerNic = prosumer.nic,
            nodeId = "node-03",
            nodeName = "Kelaniya Community Hub",
            date = "Sep 18, 2026",
            startTime = "09:00 AM",
            endTime = "10:00 AM",
            energyKwh = 6.0,
            status = ReservationStatus.PENDING,
            createdAt = "Sep 15, 2026"
        ),
        EnergyReservation(
            id = "res-0987",
            prosumerNic = prosumer.nic,
            nodeId = "node-04",
            nodeName = "Maharagama Grid Point",
            date = "Sep 10, 2026",
            startTime = "11:00 AM",
            endTime = "12:00 PM",
            energyKwh = 5.2,
            status = ReservationStatus.COMPLETED,
            createdAt = "Sep 8, 2026"
        ),
        EnergyReservation(
            id = "res-0954",
            prosumerNic = prosumer.nic,
            nodeId = "node-02",
            nodeName = "Rajagiriya Substation Node",
            date = "Sep 5, 2026",
            startTime = "04:00 PM",
            endTime = "05:00 PM",
            energyKwh = 4.0,
            status = ReservationStatus.CANCELLED,
            createdAt = "Sep 3, 2026"
        )
    )

    /** Reservations visible to the Grid Operator's assigned node, for the verify/finalize demo. */
    val operatorQueue = listOf(
        reservations[0] // the approved, QR-ready one
    )
}
