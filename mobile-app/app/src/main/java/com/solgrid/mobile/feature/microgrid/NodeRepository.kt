package com.solgrid.mobile.feature.microgrid

import com.solgrid.mobile.core.network.ApiService
import com.solgrid.mobile.core.network.NetworkModule
import retrofit2.Response
import java.io.IOException
import com.solgrid.mobile.core.storage.AppDatabase
import com.solgrid.mobile.core.network.SessionStore
import kotlinx.serialization.encodeToString
import kotlinx.serialization.decodeFromString
import kotlinx.serialization.json.Json
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

sealed interface NodeResult<out T> {
    data class Success<T>(val value: T) : NodeResult<T>
    data class Failure(val code: Int? = null, val message: String) : NodeResult<Nothing>
}

object NodeReferenceCache {
    var database: AppDatabase? = null
}

private val NEARBY_SEARCH_RADII_KM = listOf(10.0, 50.0, 200.0)

class NodeRepository(private val api: ApiService = NetworkModule.apiService) {
    private val json = Json { ignoreUnknownKeys = true }
    private suspend inline fun <reified T> cache(key: String, result: NodeResult<T>): NodeResult<T> {
        if (result is NodeResult.Success) withContext(Dispatchers.IO) {
            NodeReferenceCache.database?.cacheReference("${SessionStore.userId}:$key", json.encodeToString(result.value))
        }
        return result
    }
    suspend fun cachedStation(id: String): com.solgrid.mobile.core.network.SolarStationDto? = withContext(Dispatchers.IO) {
        NodeReferenceCache.database?.loadReference("${SessionStore.userId}:station:$id")?.let { runCatching { json.decodeFromString<com.solgrid.mobile.core.network.SolarStationDto>(it) }.getOrNull() }
    }
    suspend fun stations(search: String, status: Int?, available: Boolean?, page: Int) = cache("stations:$search:$status:$available:$page", call { api.getStations(status, search.ifBlank { null }, available, page) })

    /**
     * Searches with a tight radius first, then progressively widens if nothing is found nearby —
     * the backend caps at 200km, and a fixed 10km default was empty for real, further-apart nodes
     * (e.g. rural towns) even though the user's GPS location was resolving correctly.
     */
    suspend fun nearby(latitude: Double, longitude: Double): NodeResult<List<com.solgrid.mobile.core.network.SolarStationDto>> {
        for (radiusKilometers in NEARBY_SEARCH_RADII_KM) {
            when (val result = cache(
                "nearby:$latitude:$longitude:$radiusKilometers",
                call { api.getNearbyStations(latitude, longitude, radiusKilometers) },
            )) {
                is NodeResult.Success -> if (result.value.isNotEmpty() || radiusKilometers == NEARBY_SEARCH_RADII_KM.last()) return result
                is NodeResult.Failure -> return result
            }
        }
        error("NEARBY_SEARCH_RADII_KM must not be empty")
    }
    suspend fun station(id: String) = cache("station:$id", call { api.getStation(id) })
    suspend fun slots(stationId: String, page: Int) = cache("slots:$stationId:$page", call { api.getStationSlots(stationId, pageNumber = page) })
    suspend fun bookings(stationId: String, page: Int) = call { api.getStationReservations(stationId, pageNumber = page) }

    suspend fun allBookings(stationId: String): NodeResult<List<com.solgrid.mobile.core.network.ReservationDto>> {
        val items = mutableListOf<com.solgrid.mobile.core.network.ReservationDto>()
        var page = 1
        while (true) {
            when (val result = bookings(stationId, page)) {
                is NodeResult.Failure -> return result
                is NodeResult.Success -> {
                    items.addAll(result.value.items)
                    if (items.size >= result.value.totalCount || result.value.items.isEmpty()) return NodeResult.Success(items)
                }
            }
            page++
        }
    }

    suspend fun allSlots(stationId: String): NodeResult<List<com.solgrid.mobile.core.network.BookingSlotDto>> {
        val items = mutableListOf<com.solgrid.mobile.core.network.BookingSlotDto>()
        var page = 1
        while (true) {
            when (val result = slots(stationId, page)) {
                is NodeResult.Failure -> return result
                is NodeResult.Success -> {
                    items.addAll(result.value.items)
                    if (items.size >= result.value.totalCount || result.value.items.isEmpty()) return NodeResult.Success(items)
                }
            }
            page++
        }
    }

    suspend fun setSlotAvailability(id: String, active: Boolean): NodeResult<Unit> = try {
        val response = if (active) api.activateSlot(id) else api.deactivateSlot(id)
        if (response.isSuccessful) NodeResult.Success(Unit) else NodeResult.Failure(response.code(), com.solgrid.mobile.core.network.parseApiErrorMessage(response.errorBody()?.string(), response.code()))
    } catch (_: IOException) { NodeResult.Failure(message = "Couldn't reach the server. Try again.") }

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
