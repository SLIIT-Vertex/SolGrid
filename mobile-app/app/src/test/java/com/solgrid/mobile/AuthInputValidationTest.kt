package com.solgrid.mobile

import com.solgrid.mobile.feature.auth.hasPhoneShape
import com.solgrid.mobile.feature.auth.hasSupportedNicFormat
import com.solgrid.mobile.feature.auth.sanitizeNicInput
import com.solgrid.mobile.feature.auth.sanitizePhoneInput
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class AuthInputValidationTest {
    @Test
    fun nicInputRejectsUnsupportedCharactersAndStopsAtTwelveDigits() {
        assertEquals("200114701234", sanitizeNicInput("2001-ab1470123456"))
        assertTrue(hasSupportedNicFormat("200114701234"))
    }

    @Test
    fun nicInputSupportsLegacySuffix() {
        assertEquals("123456789V", sanitizeNicInput("123456789vabc"))
        assertTrue(hasSupportedNicFormat("123456789V"))
    }

    @Test
    fun phoneInputKeepsOnlyTheFirstTenDigits() {
        assertEquals("0771234567", sanitizePhoneInput("077-call-123456789"))
    }

    @Test
    fun phoneValidationUsesActualDigitCount() {
        assertFalse(hasPhoneShape("123"))
        assertFalse(hasPhoneShape("1234567890123456"))
        assertFalse(hasPhoneShape("7712345678"))
        assertTrue(hasPhoneShape("0771234567"))
    }
}
