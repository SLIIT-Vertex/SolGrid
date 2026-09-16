package com.solgrid.mobile.core.components

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.SheetState
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing

/** Thin wrapper around ModalBottomSheet applying SolGrid surface + padding conventions. */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AppBottomSheet(
    onDismissRequest: () -> Unit,
    modifier: Modifier = Modifier,
    sheetState: SheetState = androidx.compose.material3.rememberModalBottomSheetState(),
    content: @Composable androidx.compose.foundation.layout.ColumnScope.() -> Unit
) {
    val colors = SolGridTheme.colors
    ModalBottomSheet(
        onDismissRequest = onDismissRequest,
        sheetState = sheetState,
        containerColor = colors.surface,
        modifier = modifier
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = Spacing.lg)
                .padding(bottom = Spacing.lg)
                .navigationBarsPadding(),
            content = content
        )
    }
}
