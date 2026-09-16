package com.solgrid.mobile.core.network

import android.content.Context
import com.solgrid.mobile.core.storage.AppDatabase

/**
 * Holds the authenticated Grid Operator's session. Backed by AppDatabase (SQLite) so sign-in
 * survives process death/app restart, per the assignment's local-persistence requirement — call
 * init(context) once (see SolGridApp) before reading or writing a session.
 */
object SessionStore {
    private var database: AppDatabase? = null

    var accessToken: String? = null
        private set
    var userId: String? = null
        private set
    var userDisplayName: String? = null
        private set
    var role: String? = null
        private set

    fun init(context: Context) {
        database = AppDatabase.getInstance(context)
        database?.loadSession()?.let { session ->
            accessToken = session.accessToken
            userId = session.userId
            userDisplayName = session.displayName
            role = session.role
        }
    }

    fun save(accessToken: String, userId: String, displayName: String, role: String) {
        this.accessToken = accessToken
        this.userId = userId
        this.userDisplayName = displayName
        this.role = role
        database?.saveSession(accessToken, userId, displayName, role)
    }

    fun clear() {
        accessToken = null
        userId = null
        userDisplayName = null
        role = null
        database?.clearSession()
        database?.clearReferences()
    }
}
