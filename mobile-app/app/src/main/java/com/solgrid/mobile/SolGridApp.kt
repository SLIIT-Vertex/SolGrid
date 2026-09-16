package com.solgrid.mobile

import android.app.Application
import com.solgrid.mobile.core.network.SessionStore

class SolGridApp : Application() {
    override fun onCreate() {
        super.onCreate()
        SessionStore.init(this)
    }
}
