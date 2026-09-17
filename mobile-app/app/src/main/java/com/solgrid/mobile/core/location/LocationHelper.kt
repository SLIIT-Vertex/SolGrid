package com.solgrid.mobile.core.location

import android.annotation.SuppressLint
import android.content.Context
import com.google.android.gms.location.CurrentLocationRequest
import com.google.android.gms.location.LocationServices
import com.google.android.gms.location.Priority
import kotlinx.coroutines.tasks.await
import kotlinx.coroutines.withTimeoutOrNull

/**
 * One-shot device location fetch via the Fused Location Provider. Callers must hold
 * ACCESS_FINE_LOCATION (checked/requested at the UI layer) before calling [getCurrentLocation].
 *
 * Checks the last-known location first — usually available near-instantly from a cache — and uses
 * it immediately if present, since a nearby-search only needs an approximate origin. Only falls
 * back to waiting for a bounded-time fresh GPS fix (which can take several seconds, especially
 * indoors or on an emulator without an active location stream) when there's no cached location at
 * all, e.g. on a genuinely fresh install.
 */
class LocationHelper(context: Context) {
    private val client = LocationServices.getFusedLocationProviderClient(context.applicationContext)

    @SuppressLint("MissingPermission")
    suspend fun getCurrentLocation(): Pair<Double, Double>? {
        val lastKnown = runCatching { client.lastLocation.await() }.getOrNull()
        if (lastKnown != null) return lastKnown.latitude to lastKnown.longitude

        val fresh = runCatching {
            withTimeoutOrNull(FRESH_FIX_TIMEOUT_MS) {
                val request = CurrentLocationRequest.Builder()
                    .setPriority(Priority.PRIORITY_BALANCED_POWER_ACCURACY)
                    .build()
                client.getCurrentLocation(request, null).await()
            }
        }.getOrNull()
        return fresh?.let { it.latitude to it.longitude }
    }

    private companion object {
        const val FRESH_FIX_TIMEOUT_MS = 8_000L
    }
}
