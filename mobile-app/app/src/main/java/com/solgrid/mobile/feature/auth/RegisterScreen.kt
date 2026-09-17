package com.solgrid.mobile.feature.auth

import androidx.activity.compose.BackHandler
import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.ContentTransform
import androidx.compose.animation.SizeTransform
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.slideInHorizontally
import androidx.compose.animation.slideOutHorizontally
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Check
import androidx.compose.material.icons.outlined.ErrorOutline
import androidx.compose.material.icons.outlined.Info
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
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.components.AppDivider
import com.solgrid.mobile.core.components.AppTextField
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.PasswordField
import com.solgrid.mobile.core.components.PrimaryButton
import com.solgrid.mobile.core.components.SecondaryButton
import com.solgrid.mobile.core.components.clickableNoRipple
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.Sizing
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing

/**
 * Prosumer self-registration. Grid Operator accounts are created only by Backoffice (web).
 *
 * The form is asked one short section at a time (see [RegisterStep]) instead of as a single long
 * scroll: each section fits a screen, is validated before the next one opens, and the last one
 * lets the user review everything before committing.
 */
@Composable
fun RegisterScreen(
    viewModel: AuthViewModel,
    onRegisterSuccess: () -> Unit,
    onNavigateToLogin: () -> Unit
) {
    val colors = SolGridTheme.colors
    val state by viewModel.register.collectAsState()
    val snackbarHostState = remember { SnackbarHostState() }
    val scrollState = rememberScrollState()

    LaunchedEffect(Unit) { viewModel.resetRegisterForm() }

    LaunchedEffect(state.requestState) {
        if (state.requestState is AuthRequestState.Success) {
            snackbarHostState.showSnackbar(
                "Account created. It will go through Backoffice activation before you can sign in.",
            )
            onRegisterSuccess()
        }
    }

    // Every section starts at the top; carrying the previous section's scroll offset over would
    // drop the user into the middle of the new one.
    LaunchedEffect(state.step) { scrollState.scrollTo(0) }

    // System back walks the sections in reverse — only the first section leaves registration.
    BackHandler(enabled = state.step != RegisterStep.IDENTITY) { viewModel.onRegisterStepBack() }

    Box(modifier = Modifier.fillMaxSize()) {
        Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
            AppTopBar(
                title = "Register as Prosumer",
                onBack = { if (!viewModel.onRegisterStepBack()) onNavigateToLogin() }
            )

            RegisterStepper(
                current = state.step,
                onStepClick = viewModel::onRegisterStepSelected,
                modifier = Modifier.padding(horizontal = Spacing.xxl, vertical = Spacing.sm)
            )

            Column(
                modifier = Modifier
                    .weight(1f)
                    .fillMaxWidth()
                    .verticalScroll(scrollState)
                    .padding(horizontal = Spacing.xxl)
            ) {
                if (state.requestState is AuthRequestState.Error) {
                    RegisterErrorBanner((state.requestState as AuthRequestState.Error).message)
                }

                AnimatedContent(
                    targetState = state.step,
                    transitionSpec = {
                        // Going forward the new section arrives from the right, going back from
                        // the left, so the motion matches the direction of travel.
                        val direction = if (targetState.ordinal > initialState.ordinal) 1 else -1
                        ContentTransform(
                            targetContentEnter = slideInHorizontally { width -> direction * width } + fadeIn(),
                            initialContentExit = slideOutHorizontally { width -> -direction * width } + fadeOut(),
                            sizeTransform = SizeTransform(clip = false)
                        )
                    },
                    label = "register-section"
                ) { step ->
                    Column(modifier = Modifier.fillMaxWidth()) {
                        Text(
                            step.title,
                            style = AppType.screenTitle,
                            color = colors.textPrimary,
                            modifier = Modifier.padding(top = Spacing.md)
                        )
                        Text(
                            step.subtitle,
                            style = AppType.body,
                            color = colors.textSecondary,
                            modifier = Modifier.padding(top = Spacing.xs, bottom = Spacing.xl)
                        )

                        when (step) {
                            RegisterStep.IDENTITY -> IdentitySection(state, viewModel::onRegisterField)
                            RegisterStep.CONTACT -> ContactSection(state, viewModel::onRegisterField)
                            RegisterStep.SECURITY -> SecuritySection(state, viewModel::onRegisterField)
                            RegisterStep.REVIEW -> ReviewSection(
                                state = state,
                                onToggleTerms = viewModel::onToggleTerms,
                                onEditStep = viewModel::onRegisterStepSelected
                            )
                        }

                        if (step == RegisterStep.IDENTITY) {
                            SignInPrompt(
                                onNavigateToLogin = onNavigateToLogin,
                                modifier = Modifier.padding(top = Spacing.xxl)
                            )
                        }
                        Box(modifier = Modifier.height(Spacing.xxl))
                    }
                }
            }

            RegisterFooter(
                state = state,
                onBack = { viewModel.onRegisterStepBack() },
                onContinue = viewModel::onRegisterContinue,
                onSubmit = { viewModel.submitRegister {} }
            )
        }

        SnackbarHost(
            hostState = snackbarHostState,
            modifier = Modifier.align(Alignment.BottomCenter).padding(Spacing.md),
        )
    }
}

// ---- Sections ----

@Composable
private fun IdentitySection(state: RegisterUiState, onField: (String, String) -> Unit) {
    AppTextField(
        value = state.nic,
        onValueChange = { onField("nic", it) },
        label = "NIC number",
        placeholder = "12 digits or 9 digits + V/X",
        keyboardType = KeyboardType.Number,
        errorText = state.fieldErrors["nic"]
    )
    AppTextField(
        value = state.fullName,
        onValueChange = { onField("fullName", it) },
        label = "Full name",
        errorText = state.fieldErrors["fullName"],
        modifier = Modifier.padding(top = Spacing.md)
    )
}

@Composable
private fun ContactSection(state: RegisterUiState, onField: (String, String) -> Unit) {
    AppTextField(
        value = state.email,
        onValueChange = { onField("email", it) },
        label = "Email address",
        keyboardType = KeyboardType.Email,
        errorText = state.fieldErrors["email"]
    )
    AppTextField(
        value = state.phone,
        onValueChange = { onField("phone", it) },
        label = "Phone number",
        placeholder = "07XXXXXXXX",
        keyboardType = KeyboardType.Phone,
        errorText = state.fieldErrors["phone"],
        modifier = Modifier.padding(top = Spacing.md)
    )
}

@Composable
private fun SecuritySection(state: RegisterUiState, onField: (String, String) -> Unit) {
    val colors = SolGridTheme.colors
    PasswordField(
        value = state.password,
        onValueChange = { onField("password", it) },
        label = "Password",
        errorText = state.fieldErrors["password"]
    )

    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(top = Spacing.md)
            .clip(RoundedCornerShape(Radius.md))
            .background(colors.surfaceAlt)
            .padding(Spacing.md)
    ) {
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
        onValueChange = { onField("confirmPassword", it) },
        label = "Confirm password",
        errorText = state.fieldErrors["confirmPassword"],
        modifier = Modifier.padding(top = Spacing.md)
    )
}

@Composable
private fun ReviewSection(
    state: RegisterUiState,
    onToggleTerms: () -> Unit,
    onEditStep: (RegisterStep) -> Unit
) {
    val colors = SolGridTheme.colors

    ReviewCard(title = "Identity", onEdit = { onEditStep(RegisterStep.IDENTITY) }) {
        ReviewRow("NIC number", state.nic)
        ReviewRow("Full name", state.fullName)
    }
    ReviewCard(
        title = "Contact",
        onEdit = { onEditStep(RegisterStep.CONTACT) },
        modifier = Modifier.padding(top = Spacing.md)
    ) {
        ReviewRow("Email address", state.email)
        ReviewRow("Phone number", state.phone)
    }
    ReviewCard(
        title = "Security",
        onEdit = { onEditStep(RegisterStep.SECURITY) },
        modifier = Modifier.padding(top = Spacing.md)
    ) {
        ReviewRow("Password", "•".repeat(state.password.length.coerceAtMost(12)))
    }

    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(top = Spacing.md)
            .clip(RoundedCornerShape(Radius.md))
            .background(colors.accentSurface)
            .padding(Spacing.md),
        horizontalArrangement = Arrangement.spacedBy(Spacing.sm)
    ) {
        Icon(Icons.Outlined.Info, contentDescription = null, tint = colors.accent, modifier = Modifier.size(Sizing.iconSm))
        Text(
            "A Backoffice officer reviews and activates new accounts — you can sign in once that is done.",
            style = AppType.supporting,
            color = colors.textSecondary
        )
    }

    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(top = Spacing.md)
            .clickableNoRipple(onToggleTerms),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Checkbox(
            checked = state.agreedToTerms,
            onCheckedChange = { onToggleTerms() },
            colors = CheckboxDefaults.colors(checkedColor = colors.accent)
        )
        Text(
            "I agree to the Terms of Service and Privacy Policy",
            style = AppType.supporting,
            color = colors.textSecondary
        )
    }
    state.fieldErrors["terms"]?.let {
        Text(it, style = AppType.caption, color = colors.error, modifier = Modifier.padding(start = Spacing.md))
    }
}

@Composable
private fun ReviewCard(
    title: String,
    onEdit: () -> Unit,
    modifier: Modifier = Modifier,
    content: @Composable ColumnScope.() -> Unit
) {
    val colors = SolGridTheme.colors
    Column(
        modifier = modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(Radius.md))
            .border(Sizing.borderThin, colors.border, RoundedCornerShape(Radius.md))
            .background(colors.surface)
            .padding(Spacing.lg)
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(title, style = AppType.caption, color = colors.textTertiary)
            Text(
                "Edit",
                style = AppType.caption,
                color = colors.accent,
                modifier = Modifier.clickableNoRipple(onEdit)
            )
        }
        content()
    }
}

@Composable
private fun ReviewRow(label: String, value: String) {
    val colors = SolGridTheme.colors
    Column(modifier = Modifier.fillMaxWidth().padding(top = Spacing.md)) {
        Text(label, style = AppType.supporting, color = colors.textSecondary)
        Text(
            value.ifBlank { "—" },
            style = AppType.bodyStrong,
            color = colors.textPrimary,
            modifier = Modifier.padding(top = Spacing.xxs)
        )
    }
}

// ---- Chrome ----

private val StepCircleSize = 28.dp

/**
 * Numbered progress header. Completed steps are tappable so the user can go back and change an
 * answer without stepping through every section in between.
 */
@Composable
private fun RegisterStepper(
    current: RegisterStep,
    onStepClick: (RegisterStep) -> Unit,
    modifier: Modifier = Modifier
) {
    val colors = SolGridTheme.colors
    val steps = RegisterStep.entries
    val connectorOffset = StepCircleSize / 2 - 1.dp

    Row(modifier = modifier.fillMaxWidth()) {
        steps.forEachIndexed { index, step ->
            val done = index < current.ordinal
            val active = index == current.ordinal

            Box(
                modifier = Modifier
                    .weight(1f)
                    .then(if (done) Modifier.clickableNoRipple { onStepClick(step) } else Modifier)
            ) {
                // Connectors are drawn first so the circles paint over their ends.
                if (index > 0) {
                    StepConnector(
                        filled = index <= current.ordinal,
                        modifier = Modifier.align(Alignment.TopStart).padding(top = connectorOffset)
                    )
                }
                if (index < steps.lastIndex) {
                    StepConnector(
                        filled = index < current.ordinal,
                        modifier = Modifier.align(Alignment.TopEnd).padding(top = connectorOffset)
                    )
                }

                Column(
                    modifier = Modifier.align(Alignment.TopCenter),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    Box(
                        modifier = Modifier
                            .size(StepCircleSize)
                            .clip(CircleShape)
                            .background(if (done || active) colors.accent else colors.background)
                            .border(
                                Sizing.borderThin,
                                if (done || active) colors.accent else colors.border,
                                CircleShape
                            ),
                        contentAlignment = Alignment.Center
                    ) {
                        if (done) {
                            Icon(
                                Icons.Outlined.Check,
                                contentDescription = null,
                                tint = colors.onAccent,
                                modifier = Modifier.size(16.dp)
                            )
                        } else {
                            Text(
                                "${index + 1}",
                                style = AppType.caption,
                                color = if (active) colors.onAccent else colors.textTertiary
                            )
                        }
                    }
                    Text(
                        step.label,
                        style = AppType.caption,
                        color = if (active) colors.textPrimary else colors.textTertiary,
                        textAlign = TextAlign.Center,
                        maxLines = 1,
                        modifier = Modifier.padding(top = Spacing.xs)
                    )
                }
            }
        }
    }
}

@Composable
private fun StepConnector(filled: Boolean, modifier: Modifier = Modifier) {
    val colors = SolGridTheme.colors
    Box(
        modifier = modifier
            .fillMaxWidth(0.5f)
            .height(2.dp)
            .background(if (filled) colors.accent else colors.border)
    )
}

@Composable
private fun RegisterErrorBanner(message: String) {
    val colors = SolGridTheme.colors
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(top = Spacing.md)
            .clip(RoundedCornerShape(Radius.md))
            .background(colors.errorSurface)
            .padding(Spacing.md),
        horizontalArrangement = Arrangement.spacedBy(Spacing.sm),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(Icons.Outlined.ErrorOutline, contentDescription = null, tint = colors.error, modifier = Modifier.size(Sizing.iconSm))
        Text(message, style = AppType.supporting, color = colors.error)
    }
}

/** Actions stay pinned below the section so "Continue" is reachable without scrolling. */
@Composable
private fun RegisterFooter(
    state: RegisterUiState,
    onBack: () -> Unit,
    onContinue: () -> Unit,
    onSubmit: () -> Unit
) {
    val colors = SolGridTheme.colors
    val isFirst = state.step == RegisterStep.IDENTITY
    val isReview = state.step == RegisterStep.REVIEW

    Column(
        modifier = Modifier
            .fillMaxWidth()
            .background(colors.background)
            .navigationBarsPadding()
            .imePadding()
    ) {
        AppDivider()
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = Spacing.xxl, vertical = Spacing.lg),
            horizontalArrangement = Arrangement.spacedBy(Spacing.md)
        ) {
            if (!isFirst) {
                SecondaryButton(text = "Back", onClick = onBack, modifier = Modifier.weight(1f))
            }
            PrimaryButton(
                text = if (isReview) "Create Account" else "Continue",
                onClick = if (isReview) onSubmit else onContinue,
                loading = state.requestState is AuthRequestState.Loading,
                modifier = Modifier.weight(if (isFirst) 1f else 1.6f)
            )
        }
    }
}

@Composable
private fun SignInPrompt(onNavigateToLogin: () -> Unit, modifier: Modifier = Modifier) {
    val colors = SolGridTheme.colors
    Row(
        modifier = modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.Center
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
