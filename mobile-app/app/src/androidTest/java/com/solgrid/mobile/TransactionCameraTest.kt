package com.solgrid.mobile

import android.view.View
import androidx.activity.compose.setContent
import android.view.ViewGroup
import androidx.compose.runtime.mutableStateOf
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.lifecycle.Lifecycle
import androidx.test.platform.app.InstrumentationRegistry
import com.journeyapps.barcodescanner.BarcodeView
import com.solgrid.mobile.feature.operator.ScannerScreen
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test

class TransactionCameraTest {
    @get:Rule val compose = createAndroidComposeRule<MainActivity>()

    @Test
    fun embeddedScannerOpensInCurrentActivityAndReleasesCamera() {
        val instrumentation = InstrumentationRegistry.getInstrumentation()
        instrumentation.uiAutomation.executeShellCommand("pm grant ${instrumentation.targetContext.packageName} android.permission.CAMERA").close()
        val visible = mutableStateOf(true)
        compose.runOnUiThread { compose.activity.setContent { if (visible.value) ScannerScreen(onBack = {}, onCodeScanned = {}) } }
        var camera: BarcodeView? = null
        compose.waitUntil(10_000) {
            compose.runOnUiThread { camera = findCamera(compose.activity.window.decorView) }
            camera?.isPreviewActive == true
        }
        assertTrue("The camera view must be embedded inside MainActivity", camera != null)
        compose.activityRule.scenario.moveToState(Lifecycle.State.CREATED)
        compose.waitUntil(10_000) { camera?.isCameraClosed == true }
        compose.activityRule.scenario.moveToState(Lifecycle.State.RESUMED)
        compose.waitUntil(10_000) { camera?.isPreviewActive == true }
        compose.runOnUiThread { visible.value = false }
        compose.waitUntil(10_000) { camera?.isCameraClosed == true }
    }

    private fun findCamera(view: View): BarcodeView? {
        if (view is BarcodeView) return view
        if (view is ViewGroup) for (index in 0 until view.childCount) {
            findCamera(view.getChildAt(index))?.let { return it }
        }
        return null
    }
}
