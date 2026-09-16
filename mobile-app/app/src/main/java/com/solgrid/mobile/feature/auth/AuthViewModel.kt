package com.solgrid.mobile.feature.auth

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.solgrid.mobile.core.models.AppRole
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

sealed interface AuthRequestState {
    data object Idle : AuthRequestState
    data object Loading : AuthRequestState
    data object Success : AuthRequestState
    data class Error(val message: String) : AuthRequestState
}

data class LoginUiState(
    val role: AppRole = AppRole.PROSUMER,
    val identifier: String = "", // NIC for Prosumer, Operator ID/email for Grid Operator
    val password: String = "",
    val identifierError: String? = null,
    val passwordError: String? = null,
    val requestState: AuthRequestState = AuthRequestState.Idle
)

data class RegisterUiState(
    val nic: String = "",
    val fullName: String = "",
    val email: String = "",
    val phone: String = "",
    val address: String = "",
    val password: String = "",
    val confirmPassword: String = "",
    val agreedToTerms: Boolean = false,
    val fieldErrors: Map<String, String> = emptyMap(),
    val requestState: AuthRequestState = AuthRequestState.Idle
)

/**
 * Handles the two mobile-facing entry points: Solar Prosumer (NIC + password) and Grid Operator
 * (operator ID + password). In the real system, `POST /api/auth/login` returns the role/context
 * used to route the user to the correct mobile home — this ViewModel simulates that call.
 */
class AuthViewModel : ViewModel() {

    private val _login = MutableStateFlow(LoginUiState())
    val login: StateFlow<LoginUiState> = _login

    private val _register = MutableStateFlow(RegisterUiState())
    val register: StateFlow<RegisterUiState> = _register

    fun onRoleSelect(role: AppRole) = _login.update { it.copy(role = role, identifierError = null, passwordError = null) }
    fun onIdentifierChange(value: String) = _login.update { it.copy(identifier = value, identifierError = null) }
    fun onPasswordChange(value: String) = _login.update { it.copy(password = value, passwordError = null) }

    fun submitLogin(onSuccess: (AppRole) -> Unit) {
        val state = _login.value
        var identifierError: String? = null
        var passwordError: String? = null
        if (state.role == AppRole.PROSUMER) {
            if (state.identifier.length != 12 || !state.identifier.all { it.isDigit() }) {
                identifierError = "Enter a valid 12-digit NIC number"
            }
        } else {
            if (state.identifier.isBlank()) identifierError = "Enter your Operator ID"
        }
        if (state.password.isBlank()) passwordError = "Password is required"
        if (identifierError != null || passwordError != null) {
            _login.update { it.copy(identifierError = identifierError, passwordError = passwordError) }
            return
        }
        viewModelScope.launch {
            _login.update { it.copy(requestState = AuthRequestState.Loading) }
            delay(1000)
            if (state.password.length < 6) {
                _login.update {
                    it.copy(requestState = AuthRequestState.Error("Incorrect credentials. Please try again."))
                }
            } else {
                _login.update { it.copy(requestState = AuthRequestState.Success) }
                onSuccess(state.role)
            }
        }
    }

    fun onRegisterField(field: String, value: String) {
        _register.update {
            val updated = when (field) {
                "nic" -> it.copy(nic = value)
                "fullName" -> it.copy(fullName = value)
                "email" -> it.copy(email = value)
                "phone" -> it.copy(phone = value)
                "address" -> it.copy(address = value)
                "password" -> it.copy(password = value)
                "confirmPassword" -> it.copy(confirmPassword = value)
                else -> it
            }
            updated.copy(fieldErrors = updated.fieldErrors - field)
        }
    }

    fun onToggleTerms() = _register.update { it.copy(agreedToTerms = !it.agreedToTerms) }

    fun submitRegister(onSuccess: () -> Unit) {
        val state = _register.value
        val errors = mutableMapOf<String, String>()
        if (state.nic.length != 12 || !state.nic.all { it.isDigit() }) errors["nic"] = "NIC must be 12 digits"
        if (state.fullName.isBlank()) errors["fullName"] = "Full name is required"
        if (!state.email.contains("@")) errors["email"] = "Enter a valid email address"
        if (state.phone.isBlank()) errors["phone"] = "Phone number is required"
        if (!isPasswordStrong(state.password)) errors["password"] = "Password does not meet requirements"
        if (state.confirmPassword != state.password) errors["confirmPassword"] = "Passwords do not match"
        if (!state.agreedToTerms) errors["terms"] = "You must accept the Terms of Service"
        if (errors.isNotEmpty()) {
            _register.update { it.copy(fieldErrors = errors) }
            return
        }
        viewModelScope.launch {
            _register.update { it.copy(requestState = AuthRequestState.Loading) }
            delay(1100)
            // New prosumer accounts start PENDING until the API confirms creation — mirrors the
            // "pending activation" flow Backoffice reviews on the web side.
            _register.update { it.copy(requestState = AuthRequestState.Success) }
            onSuccess()
        }
    }
}

fun isPasswordStrong(password: String): Boolean =
    password.length >= 8 &&
        password.any { it.isUpperCase() } &&
        password.any { it.isLowerCase() } &&
        password.any { it.isDigit() } &&
        password.any { !it.isLetterOrDigit() }

data class PasswordRequirement(val label: String, val met: Boolean)

fun passwordRequirements(password: String): List<PasswordRequirement> = listOf(
    PasswordRequirement("At least 8 characters", password.length >= 8),
    PasswordRequirement("Uppercase letter", password.any { it.isUpperCase() }),
    PasswordRequirement("Lowercase letter", password.any { it.isLowerCase() }),
    PasswordRequirement("Number", password.any { it.isDigit() }),
    PasswordRequirement("Special character", password.any { !it.isLetterOrDigit() })
)
