package com.solgrid.mobile.core.qr

import com.google.zxing.BinaryBitmap
import com.google.zxing.MultiFormatReader
import com.google.zxing.RGBLuminanceSource
import com.google.zxing.common.HybridBinarizer
import org.junit.Assert.*
import org.junit.Test

class TransactionQrTest {
    @Test
    fun generatedQrDecodesToExactServerTransactionPayload() {
        val payload = TransactionQr.payload("0123456789abcdef0123456789abcdef", "Abc_-1234567890".repeat(5))
        val matrix = TransactionQr.encode(payload)
        val pixels = IntArray(matrix.width * matrix.height) { i -> if (matrix[i % matrix.width, i / matrix.width]) 0xFF000000.toInt() else 0xFFFFFFFF.toInt() }
        val decoded = MultiFormatReader().decode(BinaryBitmap(HybridBinarizer(RGBLuminanceSource(matrix.width, matrix.height, pixels))))
        assertEquals(payload, decoded.text)
        assertEquals("0123456789abcdef0123456789abcdef", TransactionQr.parse(decoded.text)!!.reservationId)
    }

    @Test
    fun malformedOrUnrelatedCodesAreRejectedBeforeVerification() {
        listOf("", "https://example.com", "id|", "|token", "id|token|extra", "id|to ken", "id|tok\nen", "a".repeat(2049)).forEach { assertNull(TransactionQr.parse(it)) }
        assertEquals(TransactionQr.Payload("id", "token"), TransactionQr.parse("id|token"))
    }
}
