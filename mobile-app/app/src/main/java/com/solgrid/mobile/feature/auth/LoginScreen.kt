package com.solgrid.mobile.feature.auth

import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
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
import androidx.compose.material.icons.outlined.ErrorOutline
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.R
import com.solgrid.mobile.core.components.AppTextField
import com.solgrid.mobile.core.components.FilterChip
import com.solgrid.mobile.core.components.PasswordField
import com.solgrid.mobile.core.components.PrimaryButton
import com.solgrid.mobile.core.components.clickableNoRipple
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.models.AppRole

@Composable
fun LoginScreen(
    viewModel: AuthViewModel,
    onLoginSuccess: (AppRole) -> Unit,
    onNavigateToRegister: () -> Unit
) {
    val colors = SolGridTheme.colors
    val state by viewModel.login.collectAsState()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(colors.background)
            .verticalScroll(rememberScrollState())
            .padding(horizontal = Spacing.xxl)
    ) {
        Box(modifier = Modifier.padding(top = Spacing.huge))
        Image(
            painter = painterResource(id = R.drawable.app_logo),
            contentDescription = "SolGrid logo",
            modifier = Modifier.size(72.dp)
        )

        Text("Welcome back", style = AppType.screenTitle, color = colors.textPrimary, modifier = Modifier.padding(top = Spacing.xxl))
        Text(
            "Sign in to continue to SolGrid.",
            style = AppType.body,
            color = colors.textSecondary,
            modifier = Modifier.padding(top = Spacing.xs)
        )

        Row(
            modifier = Modifier.fillMaxWidth().padding(top = Spacing.xl),
            horizontalArrangement = Arrangement.spacedBy(Spacing.sm)
        ) {
            FilterChip(
                label = "Solar Prosumer",
                selected = state.role == AppRole.PROSUMER,
                onClick = { viewModel.onRoleSelect(AppRole.PROSUMER) },
                modifier = Modifier.weight(1f)
            )
            FilterChip(
                label = "Grid Operator",
                selected = state.role == AppRole.GRID_OPERATOR,
                onClick = { viewModel.onRoleSelect(AppRole.GRID_OPERATOR) },
                modifier = Modifier.weight(1f)
            )
        }

        if (state.requestState is AuthRequestState.Error) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(top = Spacing.lg)
                    .clip(RoundedCornerShape(Radius.md))
                    .background(colors.errorSurface)
                    .padding(Spacing.md),
                horizontalArrangement = Arrangement.spacedBy(Spacing.sm),
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
            value = state.identifier,
            onValueChange = viewModel::onIdentifierChange,
            label = if (state.role == AppRole.PROSUMER) "NIC number" else "Email",
            placeholder = if (state.role == AppRole.PROSUMER) "e.g. 200114701234" else "you@solgrid.com",
            errorText = state.identifierError,
            keyboardType = if (state.role == AppRole.PROSUMER) KeyboardType.Number else KeyboardType.Email,
            modifier = Modifier.padding(top = Spacing.xl)
        )
        PasswordField(
            value = state.password,
            onValueChange = viewModel::onPasswordChange,
            label = "Password",
            errorText = state.passwordError,
            modifier = Modifier.padding(top = Spacing.md)
        )

        PrimaryButton(
            text = "Sign In",
            onClick = { viewModel.submitLogin(onLoginSuccess) },
            loading = state.requestState is AuthRequestState.Loading,
            modifier = Modifier.fillMaxWidth().padding(top = Spacing.xxl)
        )

        if (state.role == AppRole.PROSUMER) {
            Row(
                modifier = Modifier.fillMaxWidth().padding(vertical = Spacing.xxxl),
                horizontalArrangement = Arrangement.Center
            ) {
                Text("Don't have an account? ", style = AppType.body, color = colors.textSecondary)
                Text(
                    "Register as Prosumer",
                    style = AppType.bodyStrong,
                    color = colors.accent,
                    modifier = Modifier.clickableNoRipple(onNavigateToRegister)
                )
            }
        } else {
            Text(
                "Grid Operator accounts are created by Backoffice. Contact your administrator if you need access.",
                style = AppType.caption,
                color = colors.textTertiary,
                modifier = Modifier.padding(top = Spacing.xxxl, bottom = Spacing.xxxl)
            )
        }
    }
}
