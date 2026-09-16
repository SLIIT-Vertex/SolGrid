package com.solgrid.mobile.feature.prosumer

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CheckboxDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.getValue
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.ConfirmationDialog
import com.solgrid.mobile.core.components.PrimaryButton
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing

/** Request account deactivation (MOB-04). Only a Backoffice officer can reactivate afterwards. */
@Composable
fun DeactivationRequestScreen(viewModel: ProsumerViewModel, onBack: () -> Unit, onDeactivated: () -> Unit) {
    val colors = SolGridTheme.colors
    var confirmed by remember { mutableStateOf(false) }
    var showConfirm by remember { mutableStateOf(false) }
    var submitting by remember { mutableStateOf(false) }

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Deactivate Account", onBack = onBack)
        Column(modifier = Modifier.fillMaxSize().padding(horizontal = Spacing.lg)) {
            Text("Before you deactivate", style = AppType.screenTitle, color = colors.textPrimary, modifier = Modifier.padding(top = Spacing.lg))
            Text(
                "Deactivating your account will pause access to booking new reservations and hide your profile from active listings. Existing approved bookings are not automatically cancelled.",
                style = AppType.body,
                color = colors.textSecondary,
                modifier = Modifier.padding(top = Spacing.sm, bottom = Spacing.xl)
            )
            Text(
                "Only a Backoffice officer can reactivate your account once it is deactivated.",
                style = AppType.bodyStrong,
                color = colors.warning
            )

            Row(modifier = Modifier.fillMaxWidth().padding(top = Spacing.xl), verticalAlignment = Alignment.Top) {
                Checkbox(
                    checked = confirmed,
                    onCheckedChange = { confirmed = it },
                    colors = CheckboxDefaults.colors(checkedColor = colors.error)
                )
                Text(
                    "I understand my account will be deactivated and can only be reactivated by Backoffice.",
                    style = AppType.supporting,
                    color = colors.textSecondary,
                    modifier = Modifier.padding(top = Spacing.md)
                )
            }

            PrimaryButton(
                text = "Request Deactivation",
                destructive = true,
                enabled = confirmed,
                loading = submitting,
                onClick = { showConfirm = true },
                modifier = Modifier.fillMaxWidth().padding(top = Spacing.xxl)
            )
        }
    }

    if (showConfirm) {
        ConfirmationDialog(
            title = "Deactivate your account?",
            message = "This request will be sent to Backoffice for processing.",
            confirmText = "Deactivate",
            destructive = true,
            onConfirm = {
                showConfirm = false
                submitting = true
                viewModel.requestDeactivation {
                    submitting = false
                    onDeactivated()
                }
            },
            onDismiss = { showConfirm = false }
        )
    }
}
