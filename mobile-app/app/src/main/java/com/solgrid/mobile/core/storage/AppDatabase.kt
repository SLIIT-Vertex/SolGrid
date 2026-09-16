package com.solgrid.mobile.core.storage

import android.content.ContentValues
import android.content.Context
import android.database.sqlite.SQLiteDatabase
import android.database.sqlite.SQLiteOpenHelper

/**
 * Pure Android SQLiteOpenHelper (no Room/ORM) storing local session state, per the assignment's
 * "local SQLite database for local user management" requirement. Holds only the signed-in
 * account's session, not authoritative business data — every read/write of reservations, nodes,
 * etc. still goes through the Web API, per the FAT-service constraint.
 */
class AppDatabase private constructor(context: Context) :
    SQLiteOpenHelper(context.applicationContext, DATABASE_NAME, null, DATABASE_VERSION) {

    override fun onCreate(db: SQLiteDatabase) {
        db.execSQL(
            """
            CREATE TABLE $TABLE_SESSION (
                id INTEGER PRIMARY KEY CHECK (id = 1),
                accessToken TEXT NOT NULL,
                userId TEXT NOT NULL,
                displayName TEXT NOT NULL,
                role TEXT NOT NULL
            )
            """.trimIndent(),
        )
    }

    override fun onUpgrade(db: SQLiteDatabase, oldVersion: Int, newVersion: Int) {
        db.execSQL("DROP TABLE IF EXISTS $TABLE_SESSION")
        onCreate(db)
    }

    fun saveSession(accessToken: String, userId: String, displayName: String, role: String) {
        val values = ContentValues().apply {
            put("id", 1)
            put("accessToken", accessToken)
            put("userId", userId)
            put("displayName", displayName)
            put("role", role)
        }
        writableDatabase.insertWithOnConflict(
            TABLE_SESSION,
            null,
            values,
            SQLiteDatabase.CONFLICT_REPLACE,
        )
    }

    fun loadSession(): StoredSession? {
        readableDatabase.query(
            TABLE_SESSION,
            arrayOf("accessToken", "userId", "displayName", "role"),
            "id = 1",
            null,
            null,
            null,
            null,
        ).use { cursor ->
            if (!cursor.moveToFirst()) return null
            return StoredSession(
                accessToken = cursor.getString(0),
                userId = cursor.getString(1),
                displayName = cursor.getString(2),
                role = cursor.getString(3),
            )
        }
    }

    fun clearSession() {
        writableDatabase.delete(TABLE_SESSION, "id = 1", null)
    }

    companion object {
        private const val DATABASE_NAME = "solgrid_local.db"
        private const val DATABASE_VERSION = 1
        private const val TABLE_SESSION = "session"

        @Volatile
        private var instance: AppDatabase? = null

        fun getInstance(context: Context): AppDatabase =
            instance ?: synchronized(this) {
                instance ?: AppDatabase(context).also { instance = it }
            }
    }
}

data class StoredSession(
    val accessToken: String,
    val userId: String,
    val displayName: String,
    val role: String,
)
