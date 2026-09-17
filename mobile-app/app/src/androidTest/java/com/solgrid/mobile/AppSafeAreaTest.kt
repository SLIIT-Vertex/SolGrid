package com.solgrid.mobile

import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.text.BasicTextField
import androidx.compose.runtime.remember
import androidx.compose.runtime.mutableStateOf
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import androidx.core.view.ViewCompat
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import com.solgrid.mobile.core.components.AppSafeArea
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test

class AppSafeAreaTest {
    @get:Rule val compose = createAndroidComposeRule<MainActivity>()

    @Test
    fun contentStaysInsideSystemBarsAndAboveKeyboard() {
        compose.runOnUiThread {
            compose.activity.setContent {
                AppSafeArea {
                    Box(Modifier.fillMaxSize().testTag("safe-content")) {
                        val text = remember { mutableStateOf("Test input") }
                        BasicTextField(value = text.value, onValueChange = { text.value = it }, modifier = Modifier.fillMaxWidth().testTag("input"))
                    }
                }
            }
        }
        compose.waitForIdle()
        val before = compose.onNodeWithTag("safe-content").fetchSemanticsNode().boundsInRoot
        var top = 0
        var bottom = 0
        var height = 0
        compose.runOnUiThread {
            val decor = compose.activity.window.decorView
            val insets = ViewCompat.getRootWindowInsets(decor)!!.getInsets(WindowInsetsCompat.Type.systemBars() or WindowInsetsCompat.Type.displayCutout())
            top = insets.top
            bottom = insets.bottom
            height = decor.height
        }
        assertTrue("Content must clear the status bar/cutout", before.top >= top - 1)
        assertTrue("Content must clear the navigation bar", before.bottom <= height - bottom + 1)
        compose.onNodeWithTag("input").performClick()
        compose.runOnUiThread {
            WindowCompat.getInsetsController(compose.activity.window, compose.activity.window.decorView).show(WindowInsetsCompat.Type.ime())
        }
        var visible = false
        for (attempt in 0 until 50) {
            compose.runOnUiThread { visible = ViewCompat.getRootWindowInsets(compose.activity.window.decorView)?.isVisible(WindowInsetsCompat.Type.ime()) == true }
            if (visible && compose.onNodeWithTag("safe-content").fetchSemanticsNode().boundsInRoot.height < before.height) break
            Thread.sleep(200)
        }
        var detail = ""
        compose.runOnUiThread {
            val decor = compose.activity.window.decorView
            detail = "imeVisible=$visible ime=${ViewCompat.getRootWindowInsets(decor)?.getInsets(WindowInsetsCompat.Type.ime())} windowFocus=${decor.hasWindowFocus()}"
        }
        assertTrue("Keyboard must open: $detail", visible)
        compose.waitForIdle()
        val after = compose.onNodeWithTag("safe-content").fetchSemanticsNode().boundsInRoot
        var keyboardTop = 0
        compose.runOnUiThread {
            val decor = compose.activity.window.decorView
            keyboardTop = decor.height - ViewCompat.getRootWindowInsets(decor)!!.getInsets(WindowInsetsCompat.Type.ime()).bottom
        }
        assertTrue("Content must clear the keyboard: before=$before after=$after keyboardTop=$keyboardTop $detail", after.bottom <= keyboardTop + 1)
    }
}
