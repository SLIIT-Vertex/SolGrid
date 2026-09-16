package com.solgrid.mobile

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import com.solgrid.mobile.core.design.AppThemeMode
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.navigation.AppNavGraph

/**
 * Single Activity hosting the entire Compose UI. Screen composition lives under `core/` and
 * `feature/` — this class only wires the theme + navigation graph together.
 */
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            var themeMode by remember { mutableStateOf(AppThemeMode.SYSTEM) }
            SolGridTheme(themeMode = themeMode) {
                AppNavGraph(onThemeModeChange = { themeMode = it })
            }
        }
    }
}
