package com.solgrid.mobile.feature.operator

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.net.Uri
import android.provider.Settings
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.content.ContextCompat
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.lifecycle.compose.LocalLifecycleOwner
import com.google.zxing.BarcodeFormat
import com.journeyapps.barcodescanner.BarcodeCallback
import com.journeyapps.barcodescanner.BarcodeResult
import com.journeyapps.barcodescanner.BarcodeView
import com.journeyapps.barcodescanner.CameraPreview
import com.journeyapps.barcodescanner.DefaultDecoderFactory
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.PrimaryButton
import com.solgrid.mobile.core.components.SecondaryButton
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.qr.TransactionQr

@Composable
fun ScannerScreen(onBack: () -> Unit, onCodeScanned: (String) -> Unit) {
    val colors = SolGridTheme.colors
    val context = LocalContext.current
    val lifecycleOwner = LocalLifecycleOwner.current
    fun hasPermission() = ContextCompat.checkSelfPermission(context, Manifest.permission.CAMERA) == PackageManager.PERMISSION_GRANTED
    var permitted by remember { mutableStateOf(hasPermission()) }
    var message by remember { mutableStateOf<String?>(null) }
    var cameraAttempt by remember { mutableIntStateOf(0) }
    val permission = rememberLauncherForActivityResult(ActivityResultContracts.RequestPermission()) { granted ->
        permitted = granted
        message = if (granted) null else "Allow camera access to scan the prosumer’s transaction QR."
    }
    LaunchedEffect(Unit) { if (!permitted) permission.launch(Manifest.permission.CAMERA) }
    DisposableEffect(lifecycleOwner) {
        val observer = LifecycleEventObserver { _, event ->
            if (event == Lifecycle.Event.ON_RESUME) permitted = hasPermission()
        }
        lifecycleOwner.lifecycle.addObserver(observer)
        onDispose { lifecycleOwner.lifecycle.removeObserver(observer) }
    }
    Column(Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Scan Prosumer QR", onBack = onBack)
        Column(Modifier.weight(1f).padding(Spacing.lg), horizontalAlignment = Alignment.CenterHorizontally) {
            Text(
                "Point the camera at the prosumer’s transaction QR. The server verifies the booking before you can finalize the transfer.",
                style = AppType.body, color = colors.textSecondary, textAlign = TextAlign.Center,
                modifier = Modifier.fillMaxWidth().padding(bottom = Spacing.lg),
            )
            Box(Modifier.fillMaxWidth().weight(1f).clip(RoundedCornerShape(20.dp)).background(Color.Black), contentAlignment = Alignment.Center) {
                if (permitted) {
                    key(cameraAttempt) {
                        EmbeddedQrCamera(
                            modifier = Modifier.fillMaxSize(),
                            onCodeScanned = onCodeScanned,
                            onInvalidCode = { message = "This is not a SolGrid transaction QR. Scan the code for an approved booking." },
                            onCameraError = { message = "Camera unavailable. Close other camera apps and try again." },
                        )
                    }
                    Box(Modifier.size(240.dp).border(2.dp, colors.accent, RoundedCornerShape(16.dp)))
                } else {
                    Text("Camera permission required", style = AppType.body, color = Color.White)
                }
            }
            message?.let { Text(it, style = AppType.body, color = colors.error, modifier = Modifier.padding(top = Spacing.md)) }
            if (!permitted) {
                PrimaryButton(text = "Allow Camera", onClick = { permission.launch(Manifest.permission.CAMERA) }, modifier = Modifier.fillMaxWidth().padding(top = Spacing.md))
                SecondaryButton(text = "Camera permission settings", onClick = {
                    context.startActivity(Intent(Settings.ACTION_APPLICATION_DETAILS_SETTINGS, Uri.parse("package:${context.packageName}")))
                }, modifier = Modifier.fillMaxWidth().padding(top = Spacing.sm))
            } else if (message != null) {
                SecondaryButton(text = "Try Again", onClick = { message = null; cameraAttempt++ }, modifier = Modifier.fillMaxWidth().padding(top = Spacing.md))
            }
        }
    }
}

/** ZXing view embedded in Compose; pause/resume follows the current screen's lifecycle.
 * https://github.com/journeyapps/zxing-android-embedded#advanced-usage
 */
@Composable
internal fun EmbeddedQrCamera(
    modifier: Modifier = Modifier,
    onCodeScanned: (String) -> Unit,
    onInvalidCode: () -> Unit,
    onCameraError: () -> Unit,
) {
    val context = LocalContext.current
    val lifecycleOwner = LocalLifecycleOwner.current
    val onCode by rememberUpdatedState(onCodeScanned)
    val onInvalid by rememberUpdatedState(onInvalidCode)
    val onError by rememberUpdatedState(onCameraError)
    val camera = remember(context) {
        BarcodeView(context).apply {
            decoderFactory = DefaultDecoderFactory(listOf(BarcodeFormat.QR_CODE))
        }
    }
    DisposableEffect(camera, lifecycleOwner) {
        var delivered = false
        var lastInvalid: String? = null
        val listener = object : CameraPreview.StateListener {
            override fun previewSized() = Unit
            override fun previewStarted() = Unit
            override fun previewStopped() = Unit
            override fun cameraClosed() = Unit
            override fun cameraError(error: Exception) { camera.pause(); onError() }
        }
        camera.addStateListener(listener)
        camera.decodeContinuous(object : BarcodeCallback {
            override fun barcodeResult(result: BarcodeResult) {
                val code = result.text ?: return
                if (delivered) return
                if (TransactionQr.parse(code) == null) {
                    if (lastInvalid != code) { lastInvalid = code; onInvalid() }
                } else {
                    delivered = true
                    camera.pause()
                    onCode(code)
                }
            }
        })
        val observer = LifecycleEventObserver { _, event ->
            when (event) {
                Lifecycle.Event.ON_RESUME -> if (!delivered) camera.resume()
                Lifecycle.Event.ON_PAUSE, Lifecycle.Event.ON_STOP -> camera.pause()
                else -> Unit
            }
        }
        lifecycleOwner.lifecycle.addObserver(observer)
        if (lifecycleOwner.lifecycle.currentState.isAtLeast(Lifecycle.State.RESUMED)) camera.resume()
        onDispose {
            lifecycleOwner.lifecycle.removeObserver(observer)
            camera.stopDecoding()
            camera.pause()
        }
    }
    AndroidView(factory = { camera }, modifier = modifier)
}
