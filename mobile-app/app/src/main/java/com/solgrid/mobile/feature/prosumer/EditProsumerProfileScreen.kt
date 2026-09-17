package com.solgrid.mobile.feature.prosumer

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.KeyboardType
import com.solgrid.mobile.core.components.AppTextField
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.PrimaryButton
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.feature.auth.PROSUMER_EMAIL_MAX_LENGTH
import com.solgrid.mobile.feature.auth.PROSUMER_FULL_NAME_MAX_LENGTH
import com.solgrid.mobile.feature.auth.hasEmailShape
import com.solgrid.mobile.feature.auth.hasPhoneShape
import com.solgrid.mobile.feature.auth.sanitizePhoneInput
import com.solgrid.mobile.feature.auth.validateFullName

/** Edit own profile (MOB-04). NIC itself is not editable — it is the fixed account identity. */
@Composable
fun EditProsumerProfileScreen(
    viewModel: ProsumerViewModel,
    onBack: () -> Unit,
    onSaved: () -> Unit,
) {
    val colors = SolGridTheme.colors
    val state by viewModel.uiState.collectAsState()
    var fullName by remember { mutableStateOf(state.profile.fullName) }
    var email by remember { mutableStateOf(state.profile.email) }
    var phone by remember { mutableStateOf(state.profile.phone) }
    var nameError by remember { mutableStateOf<String?>(null) }
    var emailError by remember { mutableStateOf<String?>(null) }
    var phoneError by remember { mutableStateOf<String?>(null) }
    var submitError by remember { mutableStateOf<String?>(null) }
    var submitting by remember { mutableStateOf(false) }

    Box(modifier = Modifier.fillMaxSize()) {
        Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
            AppTopBar(title = "Edit Profile", onBack = onBack)
            Column(modifier = Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = Spacing.lg)) {
                AppTextField(
                    value = state.profile.nic,
                    onValueChange = {},
                    label = "NIC (fixed identity)",
                    enabled = false,
                    modifier = Modifier.padding(top = Spacing.lg)
                )
                AppTextField(
                    value = fullName,
                    onValueChange = {
                        fullName = it.take(PROSUMER_FULL_NAME_MAX_LENGTH)
                        nameError = null
                    },
                    label = "Full name",
                    errorText = nameError,
                    modifier = Modifier.padding(top = Spacing.md)
                )
                AppTextField(
                    value = email,
                    onValueChange = {
                        email = it.take(PROSUMER_EMAIL_MAX_LENGTH)
                        emailError = null
                    },
                    label = "Email",
                    keyboardType = KeyboardType.Email,
                    errorText = emailError,
                    modifier = Modifier.padding(top = Spacing.md)
                )
                AppTextField(
                    value = phone,
                    onValueChange = {
                        phone = sanitizePhoneInput(it)
                        phoneError = null
                    },
                    label = "Phone",
                    placeholder = "07XXXXXXXX",
                    keyboardType = KeyboardType.Phone,
                    errorText = phoneError,
                    modifier = Modifier.padding(top = Spacing.md, bottom = Spacing.huge)
                )
                submitError?.let {
                    androidx.compose.material3.Text(it, color = colors.error, modifier = Modifier.padding(bottom = Spacing.md))
                }

                PrimaryButton(
                    text = "Save Changes",
                    loading = submitting,
                    onClick = {
                        val nextNameError = validateFullName(fullName)
                        val nextEmailError = if (hasEmailShape(email)) null else "Enter a valid email address"
                        val nextPhoneError = if (phone.isBlank() || hasPhoneShape(phone)) null else "Enter 10 digits beginning with 0"
                        nameError = nextNameError
                        emailError = nextEmailError
                        phoneError = nextPhoneError
                        if (nextNameError != null || nextEmailError != null || nextPhoneError != null) {
                            return@PrimaryButton
                        }
                        submitError = null
                        submitting = true
                        val spaceIndex = fullName.trim().indexOf(' ')
                        val firstName = if (spaceIndex == -1) fullName.trim() else fullName.trim().substring(0, spaceIndex)
                        val lastName = if (spaceIndex == -1) fullName.trim() else fullName.trim().substring(spaceIndex + 1).trim()
                        viewModel.updateProfile(
                            firstName = firstName,
                            lastName = lastName,
                            email = email,
                            phone = phone,
                            onError = {
                                submitting = false
                                submitError = it
                            },
                            onDone = {
                                submitting = false
                                onSaved()
                            },
                        )
                    },
                    modifier = Modifier.fillMaxWidth().padding(bottom = Spacing.xxl)
                )
            }
        }
    }
}
