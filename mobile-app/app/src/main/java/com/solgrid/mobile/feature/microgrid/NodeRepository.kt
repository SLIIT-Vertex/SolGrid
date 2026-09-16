package com.solgrid.mobile.feature.microgrid

import com.solgrid.mobile.core.network.ApiService
import com.solgrid.mobile.core.network.NetworkModule
import retrofit2.Response
import java.io.IOException

sealed interface NodeResult<out T> {
    data class Success<T>(val value: T) : NodeResult<T>
    data class Failure(val code: Int? = null, val message: String) : NodeResult<Nothing>
}

class NodeRepository(private val api: ApiService = NetworkModule.apiService) {
    suspend fun stations(search: String, status: Int?, available: Boolean?, page: Int) = call { api.getStations(status, search.ifBlank { null }, available, page) }
    suspend fun nearby(latitude: Double, longitude: Double) = call { api.getNearbyStations(latitude, longitude) }
    suspend fun station(id: String) = call { api.getStation(id) }
    suspend fun slots(stationId: String, page: Int) = call { api.getStationSlots(stationId, pageNumber = page) }
    suspend fun bookings(stationId: String, page: Int) = call { api.getStationReservations(stationId, pageNumber = page) }

    private suspend fun <T> call(block: suspend () -> Response<T>): NodeResult<T> = try {
        val response = block()
        response.body()?.let { NodeResult.Success(it) } ?: NodeResult.Failure(response.code(), when (response.code()) {
            401 -> "Your session has expired. Please sign in again."
            403 -> "You do not have access to this information."
            404 -> "This node is no longer available."
            else -> "The server could not complete this request."
        })
    } catch (_: IOException) { NodeResult.Failure(message = "Couldn't reach the server. Check your connection.") }
}
