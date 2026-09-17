package com.solgrid.mobile.core.location

import android.annotation.SuppressLint
import android.content.Context
import com.google.android.gms.location.CurrentLocationRequest
import com.google.android.gms.location.LocationServices
import com.google.android.gms.location.Priority
import kotlinx.coroutines.tasks.await

/**
 * One-shot device location fetch via the Fused Location Provider. Callers must hold
 * ACCESS_FINE_LOCATION (checked/requested at the UI layer) before calling [getCurrentLocation].
 */
class LocationHelper(context: Context) {
    private val client = LocationServices.getFusedLocationProviderClient(context.applicationContext)

    @SuppressLint("MissingPermission")
    suspend fun getCurrentLocation(): Pair<Double, Double>? {
        val request = CurrentLocationRequest.Builder()
            .setPriority(Priority.PRIORITY_BALANCED_POWER_ACCURACY)
            .build()
        val location = client.getCurrentLocation(request, null).await() ?: return null
        return location.latitude to location.longitude
    }
}
