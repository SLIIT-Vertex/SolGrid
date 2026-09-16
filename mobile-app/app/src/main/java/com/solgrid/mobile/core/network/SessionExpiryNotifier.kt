package com.solgrid.mobile.core.network

import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.asSharedFlow

/**
 * Signals that the current session's access token was rejected by the server (expired or
 * revoked) so the navigation layer can clear local state and return to Login. Emitted by
 * NetworkModule's auth interceptor; observed once at the AppNavGraph root.
 */
object SessionExpiryNotifier {
    private val _events = MutableSharedFlow<Unit>(extraBufferCapacity = 1)
    val events: SharedFlow<Unit> = _events.asSharedFlow()

    fun notifyExpired() {
        _events.tryEmit(Unit)
    }
}
