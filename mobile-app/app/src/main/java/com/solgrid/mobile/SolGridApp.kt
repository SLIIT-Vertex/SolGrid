package com.solgrid.mobile

import android.app.Application
import android.content.pm.PackageManager
import com.google.android.libraries.places.api.Places
import com.solgrid.mobile.core.network.SessionStore

class SolGridApp : Application() {
    override fun onCreate() {
        super.onCreate()
        SessionStore.init(this)
        initializePlaces()
    }

    private fun initializePlaces() {
        if (Places.isInitialized()) return
        val apiKey = try {
            packageManager
                .getApplicationInfo(packageName, PackageManager.GET_META_DATA)
                .metaData
                ?.getString("com.google.android.geo.API_KEY")
        } catch (_: PackageManager.NameNotFoundException) {
            null
        }
        if (!apiKey.isNullOrBlank()) {
            Places.initialize(applicationContext, apiKey)
        }
    }
}
