package com.solgrid.mobile

import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.graphics.toPixelMap
import androidx.compose.ui.test.captureToImage
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithContentDescription
import com.google.zxing.BinaryBitmap
import com.google.zxing.MultiFormatReader
import com.google.zxing.RGBLuminanceSource
import com.google.zxing.common.HybridBinarizer
import com.solgrid.mobile.core.components.QrCodeVisual
import com.solgrid.mobile.core.qr.TransactionQr
import org.junit.Assert.assertEquals
import org.junit.Rule
import org.junit.Test

class TransactionQrRenderingTest {
    @get:Rule val compose = createComposeRule()

    @Test
    fun displayedQrIsScannableAtActualDeviceDensity() {
        val payload = TransactionQr.payload("0123456789abcdef0123456789abcdef", "sample-verification-token-1234567890")
        compose.setContent { QrCodeVisual(payload) }
        val image = compose.onNodeWithContentDescription("Secure reservation transaction QR code").captureToImage()
        val map = image.toPixelMap()
        val pixels = IntArray(image.width * image.height) { i -> map[i % image.width, i / image.width].toArgb() }
        val result = MultiFormatReader().decode(BinaryBitmap(HybridBinarizer(RGBLuminanceSource(image.width, image.height, pixels))))
        assertEquals(payload, result.text)
    }
}
