package com.solgrid.mobile.feature.auth

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.solgrid.mobile.core.models.AppRole
import com.solgrid.mobile.core.network.AuthRepository
import com.solgrid.mobile.core.network.LoginOutcome
import com.solgrid.mobile.core.network.ProsumerLoginOutcome
import com.solgrid.mobile.core.network.ProsumerRegisterOutcome
import com.solgrid.mobile.core.network.ProsumerRepository
import com.solgrid.mobile.core.network.SessionStore
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
    val identifier: String = "", // Email for both Prosumer and Grid Operator — role is detected server-side
    val password: String = "",
    val identifierError: String? = null,
    val passwordError: String? = null,
    val infoMessage: String? = null,
    val requestState: AuthRequestState = AuthRequestState.Idle
)

data class RegisterUiState(
    val nic: String = "",
    val fullName: String = "",
    val email: String = "",
    val phone: String = "",
    val password: String = "",
    val confirmPassword: String = "",
    val agreedToTerms: Boolean = false,
    val fieldErrors: Map<String, String> = emptyMap(),
    val requestState: AuthRequestState = AuthRequestState.Idle
)

/**
 * Handles the two mobile-facing entry points: Solar Prosumer (NIC + password) and Grid Operator
 * (email + password). Both call the real SolGrid Web API.
 */
private const val USER_ROLE_GRID_OPERATOR = 2

class AuthViewModel : ViewModel() {

    private val _login = MutableStateFlow(LoginUiState())
    val login: StateFlow<LoginUiState> = _login

    private val _register = MutableStateFlow(RegisterUiState())
    val register: StateFlow<RegisterUiState> = _register

    fun onIdentifierChange(value: String) = _login.update { it.copy(identifier = value, identifierError = null, infoMessage = null) }
    fun onPasswordChange(value: String) = _login.update { it.copy(password = value, passwordError = null, infoMessage = null) }

    /** Call on logout so a previous session's typed credentials don't linger for the next sign-in. */
    fun resetLoginForm() {
        _login.value = LoginUiState()
    }

    /** Call when the Register screen is (re)entered so stale input from a previous visit doesn't linger. */
    fun resetRegisterForm() {
        _register.value = RegisterUiState()
    }

    private val authRepository = AuthRepository()
    private val prosumerRepository = ProsumerRepository()

    /**
     * A single login form for both Grid Operator and Prosumer — the account type is not chosen by
     * the user, it is determined by which backend credential store accepts the email/password.
     * Grid Operator (the Web-user store) is tried first; if that store doesn't recognize the
     * credentials, Prosumer login is tried next. Whichever succeeds decides where the app routes.
     */
    fun submitLogin(onSuccess: (AppRole) -> Unit) {
        val state = _login.value
        var identifierError: String? = null
        var passwordError: String? = null
        if (!hasEmailShape(state.identifier)) identifierError = "Enter a valid email address"
        if (state.password.isBlank()) passwordError = "Password is required"
        if (identifierError != null || passwordError != null) {
            _login.update { it.copy(identifierError = identifierError, passwordError = passwordError) }
            return
        }

        viewModelScope.launch {
            _login.update { it.copy(requestState = AuthRequestState.Loading) }

            val gridOperatorOutcome = authRepository.login(state.identifier, state.password)
            when {
                gridOperatorOutcome is LoginOutcome.Success &&
                    gridOperatorOutcome.response.user.role == USER_ROLE_GRID_OPERATOR -> {
                    SessionStore.save(
                        accessToken = gridOperatorOutcome.response.accessToken,
                        userId = gridOperatorOutcome.response.user.id,
                        displayName = "${gridOperatorOutcome.response.user.firstName} ${gridOperatorOutcome.response.user.lastName}",
                        role = "GridOperator",
                    )
                    _login.update { it.copy(requestState = AuthRequestState.Success) }
                    onSuccess(AppRole.GRID_OPERATOR)
                }
                // Only fall through to Prosumer on a credential mismatch (401) — a Backoffice
                // account (wrong role for mobile) or inactive account (403) should surface its own
                // error rather than being silently retried against a different credential store.
                gridOperatorOutcome is LoginOutcome.Failure && gridOperatorOutcome.statusCode == 401 -> {
                    attemptProsumerLogin(state, onSuccess)
                }
                gridOperatorOutcome is LoginOutcome.Failure -> {
                    _login.update { it.copy(requestState = AuthRequestState.Error(gridOperatorOutcome.message)) }
                }
                else -> {
                    // Grid Operator credentials were valid but for a Backoffice (non-mobile) role —
                    // try Prosumer next rather than exposing that distinction to the client.
                    attemptProsumerLogin(state, onSuccess)
                }
            }
        }
    }

    private suspend fun attemptProsumerLogin(state: LoginUiState, onSuccess: (AppRole) -> Unit) {
        when (val prosumerOutcome = prosumerRepository.login(state.identifier, state.password)) {
            is ProsumerLoginOutcome.Success -> {
                SessionStore.save(
                    accessToken = prosumerOutcome.response.accessToken,
                    userId = prosumerOutcome.response.prosumer.nic,
                    displayName = "${prosumerOutcome.response.prosumer.firstName} ${prosumerOutcome.response.prosumer.lastName}",
                    role = "Prosumer",
                )
                _login.update { it.copy(requestState = AuthRequestState.Success) }
                onSuccess(AppRole.PROSUMER)
            }
            is ProsumerLoginOutcome.Failure -> {
                _login.update { it.copy(requestState = AuthRequestState.Error("Incorrect email or password.")) }
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
        if (!hasSupportedNicFormat(state.nic)) {
            errors["nic"] = "Enter a valid NIC (12 digits, or 9 digits + V/X)"
        }
        if (state.fullName.isBlank()) errors["fullName"] = "Full name is required"
        if (!hasEmailShape(state.email)) errors["email"] = "Enter a valid email address"
        if (!hasPhoneShape(state.phone)) errors["phone"] = "Enter a valid phone number"
        if (!isPasswordStrong(state.password)) errors["password"] = "Password does not meet requirements"
        if (state.confirmPassword != state.password) errors["confirmPassword"] = "Passwords do not match"
        if (!state.agreedToTerms) errors["terms"] = "You must accept the Terms of Service"
        if (errors.isNotEmpty()) {
            _register.update { it.copy(fieldErrors = errors) }
            return
        }
        viewModelScope.launch {
            _register.update { it.copy(requestState = AuthRequestState.Loading) }
            val (firstName, lastName) = splitFullName(state.fullName)
            when (
                val outcome = prosumerRepository.register(
                    nic = state.nic,
                    firstName = firstName,
                    lastName = lastName,
                    email = state.email,
                    phoneNumber = state.phone.ifBlank { null },
                    password = state.password,
                )
            ) {
                is ProsumerRegisterOutcome.Success -> {
                    // New prosumer accounts start Pending until a Backoffice officer activates
                    // them (see docs/prosumer-management.md) — registration succeeding here does
                    // not mean the account can sign in yet, so surface that on the Login screen.
                    _register.update { it.copy(requestState = AuthRequestState.Success) }
                    _login.update {
                        it.copy(
                            infoMessage = "Account created. An administrator will review and activate it before you can sign in.",
                        )
                    }
                    onSuccess()
                }
                is ProsumerRegisterOutcome.Failure -> {
                    _register.update {
                        it.copy(requestState = AuthRequestState.Error(outcome.message))
                    }
                }
            }
        }
    }
}

/** Mirrors SolGrid.Application.Prosumers.Validation.ProsumerRequestValidationRules.HasSupportedSriLankanNicFormat. */
fun hasSupportedNicFormat(nic: String): Boolean {
    val trimmed = nic.trim()
    val isLegacy = trimmed.length == 10 &&
        trimmed.take(9).all { it.isDigit() } &&
        trimmed.last().uppercaseChar().let { it == 'V' || it == 'X' }
    val isModern = trimmed.length == 12 && trimmed.all { it.isDigit() }
    return isLegacy || isModern
}

/** Mirrors SolGrid.Application.Users.Validation.UserRequestValidationRules.HasEmailShape (minimal shape check). */
fun hasEmailShape(email: String): Boolean {
    val trimmed = email.trim()
    val atIndex = trimmed.indexOf('@')
    return atIndex > 0 && trimmed.indexOf('.', atIndex) > atIndex + 1 && trimmed.last() != '.'
}

/** Mirrors SolGrid.Application.Prosumers.Validation.ProsumerRequestValidationRules.HasPhoneNumberShape. */
fun hasPhoneShape(phone: String): Boolean {
    if (phone.isBlank()) return false
    val trimmed = phone.trim()
    return trimmed.length in 7..20 && trimmed.all { it.isDigit() || it in " +-()" }
}

private fun splitFullName(fullName: String): Pair<String, String> {
    val trimmed = fullName.trim()
    val spaceIndex = trimmed.indexOf(' ')
    return if (spaceIndex == -1) {
        trimmed to trimmed
    } else {
        trimmed.substring(0, spaceIndex) to trimmed.substring(spaceIndex + 1).trim()
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
