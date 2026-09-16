package com.solgrid.mobile.feature.prosumer

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.KeyboardType
import com.solgrid.mobile.core.components.AppTextField
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.PrimaryButton
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import kotlinx.coroutines.launch

/** Edit own profile (MOB-04). NIC itself is not editable — it is the fixed account identity. */
@Composable
fun EditProsumerProfileScreen(viewModel: ProsumerViewModel, onBack: () -> Unit) {
    val colors = SolGridTheme.colors
    val state by viewModel.uiState.collectAsState()
    var fullName by remember { mutableStateOf(state.profile.fullName) }
    var email by remember { mutableStateOf(state.profile.email) }
    var phone by remember { mutableStateOf(state.profile.phone) }
    var address by remember { mutableStateOf(state.profile.address) }
    var nameError by remember { mutableStateOf<String?>(null) }
    val snackbarHostState = remember { SnackbarHostState() }
    val scope = rememberCoroutineScope()

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
                    onValueChange = { fullName = it; nameError = null },
                    label = "Full name",
                    errorText = nameError,
                    modifier = Modifier.padding(top = Spacing.md)
                )
                AppTextField(value = email, onValueChange = { email = it }, label = "Email", keyboardType = KeyboardType.Email, modifier = Modifier.padding(top = Spacing.md))
                AppTextField(value = phone, onValueChange = { phone = it }, label = "Phone", keyboardType = KeyboardType.Phone, modifier = Modifier.padding(top = Spacing.md))
                AppTextField(
                    value = address,
                    onValueChange = { address = it },
                    label = "Address",
                    singleLine = false,
                    minLines = 2,
                    modifier = Modifier.padding(top = Spacing.md, bottom = Spacing.huge)
                )

                PrimaryButton(
                    text = "Save Changes",
                    onClick = {
                        if (fullName.isBlank()) {
                            nameError = "Full name is required"
                            return@PrimaryButton
                        }
                        viewModel.updateProfile(fullName, email, phone, address)
                        scope.launch { snackbarHostState.showSnackbar("Profile updated successfully") }
                    },
                    modifier = Modifier.fillMaxWidth().padding(bottom = Spacing.xxl)
                )
            }
        }
        SnackbarHost(hostState = snackbarHostState, modifier = Modifier.align(Alignment.BottomCenter).padding(Spacing.md))
    }
}
