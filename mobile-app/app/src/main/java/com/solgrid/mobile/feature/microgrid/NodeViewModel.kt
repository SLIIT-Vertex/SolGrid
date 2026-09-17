package com.solgrid.mobile.feature.microgrid

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.solgrid.mobile.core.network.BookingSlotDto
import com.solgrid.mobile.core.network.ReservationDto
import com.solgrid.mobile.core.network.SolarStationDto
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class NodeState(
    val nodes: List<SolarStationDto> = emptyList(), val totalCount: Long = 0,
    val selected: SolarStationDto? = null, val slots: List<BookingSlotDto> = emptyList(),
    val bookings: List<ReservationDto> = emptyList(), val loading: Boolean = false,
    val error: String? = null, val nodePage: Int = 1, val nodePageSize: Int = 20,
    val slotTotalCount: Long = 0, val bookingTotalCount: Long = 0,
    val searchLatitude: Double? = null, val searchLongitude: Double? = null,
    val searchAreaLabel: String? = null, val changingSlot: String? = null,
    val locationUnavailable: Boolean = false,
)

class NodeViewModel(private val repository: NodeRepository = NodeRepository()) : ViewModel() {
    private val _state = MutableStateFlow(NodeState())
    val state: StateFlow<NodeState> = _state

    fun loadOperatorNodes(search: String = "", status: Int? = null, available: Boolean? = null, page: Int = 1) = viewModelScope.launch {
        _state.update { it.copy(loading = true, error = null) }
        when (val result = repository.stations(search, status, available, page)) {
            is NodeResult.Success -> _state.update { it.copy(nodes = result.value.items, totalCount = result.value.totalCount, nodePage = result.value.pageNumber, nodePageSize = result.value.pageSize, loading = false) }
            is NodeResult.Failure -> _state.update { it.copy(loading = false, error = result.message) }
        }
    }
    fun loadNearby(latitude: Double, longitude: Double, areaLabel: String? = null) = viewModelScope.launch {
        _state.update {
            it.copy(
                loading = true, error = null, locationUnavailable = false,
                searchAreaLabel = areaLabel, searchLatitude = latitude, searchLongitude = longitude,
            )
        }
        when (val result = repository.nearby(latitude, longitude)) {
            is NodeResult.Success -> _state.update { it.copy(nodes = result.value, loading = false) }
            is NodeResult.Failure -> _state.update { it.copy(loading = false, error = result.message) }
        }
    }

    /** Call when the device's location genuinely could not be resolved (no fresh fix, no cached
     * last-known location) — shows an explicit "couldn't find your location" state instead of
     * silently substituting an unrelated fallback point as if it were a real "no nodes" result. */
    fun markLocationUnavailable() {
        _state.update {
            it.copy(
                loading = false, error = null, locationUnavailable = true,
                nodes = emptyList(), searchLatitude = null, searchLongitude = null, searchAreaLabel = null,
            )
        }
    }
    fun loadDetail(id: String) = viewModelScope.launch {
        _state.update { it.copy(loading = true, error = null) }
        when (val result = repository.station(id)) {
            is NodeResult.Success -> _state.update { it.copy(selected = result.value, loading = false) }
            is NodeResult.Failure -> _state.update { it.copy(selected = null, loading = false, error = result.message) }
        }
        val slots = mutableListOf<BookingSlotDto>()
        var page = 1
        while (true) {
            when (val result = repository.slots(id, page)) {
                is NodeResult.Success -> {
                    slots.addAll(result.value.items)
                    if (slots.size >= result.value.totalCount || result.value.items.isEmpty()) {
                        _state.update { it.copy(slots = slots, slotTotalCount = result.value.totalCount) }
                        break
                    }
                }
                is NodeResult.Failure -> { _state.update { it.copy(slots = emptyList(), error = result.message) }; break }
            }
            page++
        }
    }
    fun loadBookings(id: String) = viewModelScope.launch { when (val result = repository.allBookings(id)) { is NodeResult.Success -> _state.update { it.copy(bookings = result.value, bookingTotalCount = result.value.size.toLong()) }; is NodeResult.Failure -> _state.update { it.copy(error = result.message) } } }
    fun setSlotAvailability(nodeId: String, slotId: String, active: Boolean) = viewModelScope.launch {
        _state.update { it.copy(changingSlot = slotId, error = null) }
        when (val result = repository.setSlotAvailability(slotId, active)) {
            is NodeResult.Success -> { loadDetail(nodeId).join(); loadOperatorNodes().join() }
            is NodeResult.Failure -> _state.update { it.copy(error = result.message) }
        }
        _state.update { it.copy(changingSlot = null) }
    }
    fun clear() { _state.value = NodeState() }
}
