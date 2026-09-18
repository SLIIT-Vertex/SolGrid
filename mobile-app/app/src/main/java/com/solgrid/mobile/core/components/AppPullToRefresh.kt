package com.solgrid.mobile.core.components

import androidx.compose.foundation.layout.BoxScope
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier

/**
 * App-level pull-to-refresh wrapper. Wrap a screen's scrollable content in this to let the user
 * swipe down to re-fetch from the Web API. Pass the screen's existing loading flag as [refreshing]
 * so the spinner reflects real network state, and [onRefresh] to the relevant ViewModel reload.
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AppPullToRefresh(
    refreshing: Boolean,
    onRefresh: () -> Unit,
    modifier: Modifier = Modifier,
    content: @Composable BoxScope.() -> Unit,
) {
    PullToRefreshBox(
        isRefreshing = refreshing,
        onRefresh = onRefresh,
        modifier = modifier,
        content = content,
    )
}
