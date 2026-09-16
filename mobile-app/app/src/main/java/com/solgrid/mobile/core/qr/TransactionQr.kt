package com.solgrid.mobile.core.qr

import com.google.zxing.BarcodeFormat
import com.google.zxing.EncodeHintType
import com.google.zxing.common.BitMatrix
import com.google.zxing.qrcode.QRCodeWriter
import com.google.zxing.qrcode.decoder.ErrorCorrectionLevel

/** Transport encoding only. Token validity, ownership and expiry are checked by the API. */
object TransactionQr {
    data class Payload(val reservationId: String, val verificationToken: String)

    fun parse(value: String): Payload? {
        if (value.length > 2048) return null
        val parts = value.split('|')
        if (parts.size != 2 || parts.any { it.isBlank() || it.any(Char::isWhitespace) || it.any { char -> char.isISOControl() } }) return null
        return Payload(parts[0], parts[1])
    }

    fun payload(reservationId: String, verificationToken: String): String {
        val value = "$reservationId|$verificationToken"
        require(parse(value) != null) { "Invalid transaction payload" }
        return value
    }

    // ZXing encoder API: https://github.com/zxing/zxing/tree/master/core/src/main/java/com/google/zxing/qrcode
    fun encode(payload: String, pixels: Int = 512): BitMatrix {
        require(parse(payload) != null) { "Invalid transaction payload" }
        return QRCodeWriter().encode(payload, BarcodeFormat.QR_CODE, pixels, pixels, mapOf(
            EncodeHintType.CHARACTER_SET to "UTF-8",
            EncodeHintType.ERROR_CORRECTION to ErrorCorrectionLevel.M,
            EncodeHintType.MARGIN to 4,
        ))
    }
}
