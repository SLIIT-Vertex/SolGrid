package com.solgrid.mobile.feature.auth

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Check
import androidx.compose.material.icons.outlined.ErrorOutline
import androidx.compose.material.icons.outlined.RadioButtonUnchecked
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CheckboxDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.components.AppTextField
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.PasswordField
import com.solgrid.mobile.core.components.PrimaryButton
import com.solgrid.mobile.core.components.clickableNoRipple
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import kotlinx.coroutines.launch

/** Prosumer self-registration. Grid Operator accounts are created only by Backoffice (web). */
@Composable
fun RegisterScreen(
    viewModel: AuthViewModel,
    onRegisterSuccess: () -> Unit,
    onNavigateToLogin: () -> Unit
) {
    val colors = SolGridTheme.colors
    val state by viewModel.register.collectAsState()
    val snackbarHostState = remember { SnackbarHostState() }
    val scope = rememberCoroutineScope()

    LaunchedEffect(Unit) { viewModel.resetRegisterForm() }

    LaunchedEffect(state.requestState) {
        if (state.requestState is AuthRequestState.Success) {
            snackbarHostState.showSnackbar(
                "Account created. It will go through Backoffice activation before you can sign in.",
            )
            onRegisterSuccess()
        }
    }

    Box(modifier = Modifier.fillMaxSize()) {
    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Register as Prosumer", onBack = onNavigateToLogin)
        Column(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = Spacing.xxl)
        ) {
            Text(
                "Your NIC is used as your unique account identity across the system.",
                style = AppType.body,
                color = colors.textSecondary,
                modifier = Modifier.padding(top = Spacing.md, bottom = Spacing.xl)
            )

            if (state.requestState is AuthRequestState.Error) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(bottom = Spacing.lg)
                        .clip(RoundedCornerShape(Radius.md))
                        .background(colors.errorSurface)
                        .padding(Spacing.md),
                    horizontalArrangement = androidx.compose.foundation.layout.Arrangement.spacedBy(Spacing.sm),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Icon(Icons.Outlined.ErrorOutline, contentDescription = null, tint = colors.error, modifier = Modifier.size(18.dp))
                    Text(
                        (state.requestState as AuthRequestState.Error).message,
                        style = AppType.supporting,
                        color = colors.error
                    )
                }
            }

            AppTextField(
                value = state.nic,
                onValueChange = { viewModel.onRegisterField("nic", it) },
                label = "NIC number",
                placeholder = "12-digit NIC",
                keyboardType = KeyboardType.Number,
                errorText = state.fieldErrors["nic"]
            )
            AppTextField(
                value = state.fullName,
                onValueChange = { viewModel.onRegisterField("fullName", it) },
                label = "Full name",
                errorText = state.fieldErrors["fullName"],
                modifier = Modifier.padding(top = Spacing.md)
            )
            AppTextField(
                value = state.email,
                onValueChange = { viewModel.onRegisterField("email", it) },
                label = "Email address",
                keyboardType = KeyboardType.Email,
                errorText = state.fieldErrors["email"],
                modifier = Modifier.padding(top = Spacing.md)
            )
            AppTextField(
                value = state.phone,
                onValueChange = { viewModel.onRegisterField("phone", it) },
                label = "Phone number",
                keyboardType = KeyboardType.Phone,
                errorText = state.fieldErrors["phone"],
                modifier = Modifier.padding(top = Spacing.md)
            )
            PasswordField(
                value = state.password,
                onValueChange = { viewModel.onRegisterField("password", it) },
                label = "Password",
                errorText = state.fieldErrors["password"],
                modifier = Modifier.padding(top = Spacing.md)
            )

            Column(modifier = Modifier.padding(top = Spacing.md)) {
                passwordRequirements(state.password).forEach { req ->
                    Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.padding(vertical = 2.dp)) {
                        Icon(
                            if (req.met) Icons.Outlined.Check else Icons.Outlined.RadioButtonUnchecked,
                            contentDescription = null,
                            tint = if (req.met) colors.success else colors.textTertiary,
                            modifier = Modifier.size(14.dp)
                        )
                        Text(
                            req.label,
                            style = AppType.caption,
                            color = if (req.met) colors.textPrimary else colors.textTertiary,
                            modifier = Modifier.padding(start = Spacing.xs)
                        )
                    }
                }
            }

            PasswordField(
                value = state.confirmPassword,
                onValueChange = { viewModel.onRegisterField("confirmPassword", it) },
                label = "Confirm password",
                errorText = state.fieldErrors["confirmPassword"],
                modifier = Modifier.padding(top = Spacing.md)
            )

            Row(modifier = Modifier.fillMaxWidth().padding(top = Spacing.lg), verticalAlignment = Alignment.Top) {
                Checkbox(
                    checked = state.agreedToTerms,
                    onCheckedChange = { viewModel.onToggleTerms() },
                    colors = CheckboxDefaults.colors(checkedColor = colors.accent)
                )
                Text(
                    "I agree to the Terms of Service and Privacy Policy",
                    style = AppType.supporting,
                    color = colors.textSecondary,
                    modifier = Modifier.padding(top = Spacing.md)
                )
            }
            state.fieldErrors["terms"]?.let {
                Text(it, style = AppType.caption, color = colors.error, modifier = Modifier.padding(start = Spacing.lg))
            }

            PrimaryButton(
                text = "Create Account",
                onClick = { viewModel.submitRegister {} },
                loading = state.requestState is AuthRequestState.Loading,
                modifier = Modifier.fillMaxWidth().padding(top = Spacing.xl)
            )

            Row(
                modifier = Modifier.fillMaxWidth().padding(vertical = Spacing.xxxl),
                horizontalArrangement = androidx.compose.foundation.layout.Arrangement.Center
            ) {
                Text("Already have an account? ", style = AppType.body, color = colors.textSecondary)
                Text(
                    "Sign In",
                    style = AppType.bodyStrong,
                    color = colors.accent,
                    modifier = Modifier.clickableNoRipple(onNavigateToLogin)
                )
            }
        }
    }
        SnackbarHost(
            hostState = snackbarHostState,
            modifier = Modifier.align(Alignment.BottomCenter).padding(Spacing.md),
        )
    }
}
