package com.solgrid.mobile.core.components

import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.SolGridTheme

/** Reusable confirmation dialog for destructive / important actions. */
@Composable
fun ConfirmationDialog(
    title: String,
    message: String,
    confirmText: String,
    onConfirm: () -> Unit,
    onDismiss: () -> Unit,
    destructive: Boolean = false,
    dismissText: String = "Cancel"
) {
    val colors = SolGridTheme.colors
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(title, style = AppType.sectionTitle, color = colors.textPrimary) },
        text = { Text(message, style = AppType.body, color = colors.textSecondary) },
        confirmButton = {
            androidx.compose.material3.TextButton(onClick = onConfirm) {
                Text(confirmText, style = AppType.buttonLabel, color = if (destructive) colors.error else colors.accent)
            }
        },
        dismissButton = {
            androidx.compose.material3.TextButton(onClick = onDismiss) {
                Text(dismissText, style = AppType.buttonLabel, color = colors.textSecondary)
            }
        },
        containerColor = colors.surface
    )
}
