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
import androidx.compose.runtime.collectAsState
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import com.solgrid.mobile.core.components.AppTextField
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
import com.solgrid.mobile.core.models.ProsumerAccountStatus

@Composable
fun DeactivationRequestScreen(viewModel: ProsumerViewModel, onBack: () -> Unit, onDeactivated: () -> Unit) {
    val colors = SolGridTheme.colors
    val state by viewModel.uiState.collectAsState()
    val expectedVersion = remember { state.profile.version }
    var reason by remember { mutableStateOf("") }
    var confirmed by remember { mutableStateOf(false) }
    var showConfirm by remember { mutableStateOf(false) }
    var submitting by remember { mutableStateOf(false) }
    var submitError by remember { mutableStateOf<String?>(null) }

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Deactivate Account", onBack = onBack)
        Column(modifier = Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = Spacing.lg)) {
            Text("Before you deactivate", style = AppType.screenTitle, color = colors.textPrimary, modifier = Modifier.padding(top = Spacing.lg))
            Text(
                "Your request goes to Backoffice for review. You can keep using your account until Backoffice deactivates it. Deactivation ends account access; existing bookings are not automatically cancelled.",
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
                    "I understand Backoffice will review my request and only Backoffice can reactivate a deactivated account.",
                    style = AppType.supporting,
                    color = colors.textSecondary,
                    modifier = Modifier.padding(top = Spacing.md)
                )
            }

            AppTextField(value = reason, onValueChange = { reason = it.take(500) },
                label = "Reason for deactivation", placeholder = "Tell Backoffice why you want to deactivate",
                modifier = Modifier.padding(top = Spacing.md))

            submitError?.let {
                Text(it, style = AppType.supporting, color = colors.error, modifier = Modifier.padding(top = Spacing.md))
            }

            PrimaryButton(
                text = "Request Deactivation",
                destructive = true,
                enabled = confirmed && reason.isNotBlank() && state.profile.status == ProsumerAccountStatus.ACTIVE,
                loading = submitting,
                onClick = { showConfirm = true },
                modifier = Modifier.fillMaxWidth().padding(top = Spacing.xxl)
            )
        }
    }

    if (showConfirm) {
        ConfirmationDialog(
            title = "Request deactivation?",
            message = "This request will be sent to Backoffice for processing.",
            confirmText = "Send request",
            destructive = true,
            onConfirm = {
                showConfirm = false
                submitting = true
                submitError = null
                viewModel.requestDeactivation(
                    expectedVersion = expectedVersion,
                    reason = reason.trim(),
                    onError = {
                        submitting = false
                        submitError = it
                    },
                    onDone = {
                        submitting = false
                        onDeactivated()
                    },
                )
            },
            onDismiss = { showConfirm = false }
        )
    }
}
