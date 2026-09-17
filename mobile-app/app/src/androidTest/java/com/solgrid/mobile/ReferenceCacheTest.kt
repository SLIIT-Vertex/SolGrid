package com.solgrid.mobile

import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.ext.junit.runners.AndroidJUnit4
import com.solgrid.mobile.core.storage.AppDatabase
import org.junit.Assert.assertEquals
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class ReferenceCacheTest {
    @Test
    fun referenceDataRoundTripsAndUpdatesWithoutChangingSession() {
        val context = InstrumentationRegistry.getInstrumentation().targetContext
        val database = AppDatabase.getInstance(context)
        val before = database.loadSession()
        database.cacheReference("test:station", "first")
        assertEquals("first", database.loadReference("test:station"))
        database.cacheReference("test:station", "updated")
        assertEquals("updated", database.loadReference("test:station"))
        assertEquals(before, database.loadSession())
        database.writableDatabase.delete("reference_cache", "cacheKey = ?", arrayOf("test:station"))
    }
}
