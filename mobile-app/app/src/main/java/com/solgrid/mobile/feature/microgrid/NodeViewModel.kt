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
    fun loadNearby(latitude: Double = 6.9271, longitude: Double = 79.8612) = viewModelScope.launch {
        _state.update { it.copy(loading = true, error = null) }
        when (val result = repository.nearby(latitude, longitude)) {
            is NodeResult.Success -> _state.update { it.copy(nodes = result.value, loading = false) }
            is NodeResult.Failure -> _state.update { it.copy(loading = false, error = result.message) }
        }
    }
    fun loadDetail(id: String) = viewModelScope.launch {
        _state.update { it.copy(loading = true, error = null) }
        when (val result = repository.station(id)) {
            is NodeResult.Success -> _state.update { it.copy(selected = result.value, loading = false) }
            is NodeResult.Failure -> _state.update { it.copy(selected = null, loading = false, error = result.message) }
        }
        when (val result = repository.slots(id, 1)) { is NodeResult.Success -> _state.update { it.copy(slots = result.value.items, slotTotalCount = result.value.totalCount) }; is NodeResult.Failure -> Unit }
    }
    fun loadBookings(id: String) = viewModelScope.launch { when (val result = repository.bookings(id, 1)) { is NodeResult.Success -> _state.update { it.copy(bookings = result.value.items, bookingTotalCount = result.value.totalCount) }; is NodeResult.Failure -> _state.update { it.copy(error = result.message) } } }
    fun clear() { _state.value = NodeState() }
}
