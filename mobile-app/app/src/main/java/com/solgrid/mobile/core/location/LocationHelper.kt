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
 * A fresh GPS fix (getCurrentLocation) can take a while to settle, especially indoors or on an
 * emulator without an active location stream, and Play Services silently returns null rather than
 * erroring if it can't resolve one in time. To avoid the caller waiting indefinitely or bouncing
 * straight to a hardcoded fallback on a slow-but-working fix, this tries a bounded-time fresh fix
 * first, then falls back to the provider's last known location (which can be stale but is usually
 * still a reasonable "nearby" origin) before giving up.
 */
class LocationHelper(context: Context) {
    private val client = LocationServices.getFusedLocationProviderClient(context.applicationContext)

    @SuppressLint("MissingPermission")
    suspend fun getCurrentLocation(): Pair<Double, Double>? {
        val fresh = runCatching {
            withTimeoutOrNull(FRESH_FIX_TIMEOUT_MS) {
                val request = CurrentLocationRequest.Builder()
                    .setPriority(Priority.PRIORITY_BALANCED_POWER_ACCURACY)
                    .build()
                client.getCurrentLocation(request, null).await()
            }
        }.getOrNull()
        if (fresh != null) return fresh.latitude to fresh.longitude

        val lastKnown = runCatching { client.lastLocation.await() }.getOrNull()
        return lastKnown?.let { it.latitude to it.longitude }
    }

    private companion object {
        const val FRESH_FIX_TIMEOUT_MS = 8_000L
    }
}
