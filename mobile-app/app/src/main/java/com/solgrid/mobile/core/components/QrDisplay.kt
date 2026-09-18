package com.solgrid.mobile.core.components

import android.graphics.Bitmap
import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.size
import androidx.compose.runtime.Composable
import androidx.compose.runtime.produceState
import androidx.compose.runtime.getValue
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.foundation.layout.Box
import androidx.compose.ui.Alignment
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.FilterQuality
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.qr.TransactionQr

/** Real black/white QR with a four-module quiet zone, without cropping or rounded corners. */
@Composable
fun QrCodeVisual(payload: String, modifier: Modifier = Modifier, size: Dp = 220.dp) {
    val bitmap by produceState<ImageBitmap?>(initialValue = null, key1 = payload) {
        value = null
        value = withContext(Dispatchers.Default) {
            val matrix = TransactionQr.encode(payload)
            val pixels = IntArray(matrix.width * matrix.height) { index ->
                if (matrix[index % matrix.width, index / matrix.width]) android.graphics.Color.BLACK else android.graphics.Color.WHITE
            }
            Bitmap.createBitmap(pixels, matrix.width, matrix.height, Bitmap.Config.ARGB_8888).asImageBitmap()
        }
    }
    val image = bitmap
    if (image == null) {
        Box(modifier = modifier.size(size), contentAlignment = Alignment.Center) {
            CircularProgressIndicator()
        }
        return
    }
    Image(
        bitmap = image,
        contentDescription = "Secure reservation transaction QR code",
        modifier = modifier.size(size),
        filterQuality = FilterQuality.None,
    )
}
