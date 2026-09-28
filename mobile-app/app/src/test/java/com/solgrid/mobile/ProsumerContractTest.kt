package com.solgrid.mobile

import com.solgrid.mobile.core.network.ProsumerLifecycleRequestDto
import com.solgrid.mobile.core.network.UpdateProsumerRequestDto
import com.solgrid.mobile.core.network.ProsumerResponseDto
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import org.junit.Assert.*
import org.junit.Test

class ProsumerContractTest {
    @Test
    fun lifecycleCommandIncludesZeroVersionAndReason() {
        val body = Json.parseToJsonElement(Json.encodeToString(ProsumerLifecycleRequestDto(0, "Moving home"))).jsonObject
        assertEquals("0", body.getValue("expectedVersion").jsonPrimitive.content)
        assertEquals("Moving home", body.getValue("reason").jsonPrimitive.content)
    }

    @Test
    fun profileUpdateCarriesReviewedVersionWithoutAdministrativeFields() {
        val body = Json.parseToJsonElement(Json.encodeToString(UpdateProsumerRequestDto(7, "Test", "Owner", "test@example.com"))).jsonObject
        assertEquals("7", body.getValue("expectedVersion").jsonPrimitive.content)
        assertFalse(body.containsKey("nic"))
        assertFalse(body.containsKey("status"))
    }

    @Test
    fun profileResponseRetainsServerVersionAndRequestedStatus() {
        val response = Json.decodeFromString<ProsumerResponseDto>("""{"nic":"199012345678","firstName":"Test","lastName":"Owner","email":"test@example.com","status":3,"version":9,"createdAt":"2026-09-28T00:00:00Z","updatedAt":"2026-09-28T00:01:00Z"}""")
        assertEquals(9L, response.version)
        assertEquals(3, response.status)
    }
}
