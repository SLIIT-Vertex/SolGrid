package com.solgrid.mobile.core.network

import java.io.IOException

sealed interface ProsumerRegisterOutcome {
    data class Success(val response: ProsumerResponseDto) : ProsumerRegisterOutcome
    data class Failure(val message: String) : ProsumerRegisterOutcome
}

sealed interface ProsumerLoginOutcome {
    data class Success(val response: ProsumerLoginResponseDto) : ProsumerLoginOutcome
    data class Failure(val message: String) : ProsumerLoginOutcome
}

sealed interface ProsumerProfileOutcome {
    data class Success(val response: ProsumerResponseDto) : ProsumerProfileOutcome
    data class Failure(val message: String) : ProsumerProfileOutcome
}

sealed interface ProsumerActionOutcome {
    data object Success : ProsumerActionOutcome
    data class Failure(val message: String) : ProsumerActionOutcome
}

/** Wraps the Retrofit calls under api/v1/prosumers for the mobile Prosumer self-service flows. */
class ProsumerRepository(private val apiService: ApiService = NetworkModule.apiService) {

    suspend fun register(
        nic: String,
        firstName: String,
        lastName: String,
        email: String,
        phoneNumber: String?,
        password: String,
    ): ProsumerRegisterOutcome = runCatching {
        val response = apiService.registerProsumer(
            RegisterProsumerRequestDto(
                nic = nic,
                firstName = firstName,
                lastName = lastName,
                email = email,
                phoneNumber = phoneNumber,
                password = password,
            ),
        )
        val body = response.body()
        if (response.isSuccessful && body != null) {
            ProsumerRegisterOutcome.Success(body)
        } else {
            ProsumerRegisterOutcome.Failure(parseApiErrorMessage(response.errorBody()?.string(), response.code()))
        }
    }.getOrElse { error -> ProsumerRegisterOutcome.Failure(error.toFriendlyMessage()) }

    suspend fun login(nic: String, password: String): ProsumerLoginOutcome = runCatching {
        val response = apiService.loginProsumer(LoginRequestDto(email = nic, password = password))
        val body = response.body()
        if (response.isSuccessful && body != null) {
            ProsumerLoginOutcome.Success(body)
        } else {
            ProsumerLoginOutcome.Failure(parseApiErrorMessage(response.errorBody()?.string(), response.code()))
        }
    }.getOrElse { error -> ProsumerLoginOutcome.Failure(error.toFriendlyMessage()) }

    suspend fun getMyProfile(): ProsumerProfileOutcome = runCatching {
        val response = apiService.getMyProsumerProfile()
        val body = response.body()
        if (response.isSuccessful && body != null) {
            ProsumerProfileOutcome.Success(body)
        } else {
            ProsumerProfileOutcome.Failure(parseApiErrorMessage(response.errorBody()?.string(), response.code()))
        }
    }.getOrElse { error -> ProsumerProfileOutcome.Failure(error.toFriendlyMessage()) }

    suspend fun updateMyProfile(
        firstName: String,
        lastName: String,
        email: String,
        phoneNumber: String?,
    ): ProsumerProfileOutcome = runCatching {
        val response = apiService.updateMyProsumerProfile(
            UpdateProsumerRequestDto(firstName = firstName, lastName = lastName, email = email, phoneNumber = phoneNumber),
        )
        val body = response.body()
        if (response.isSuccessful && body != null) {
            ProsumerProfileOutcome.Success(body)
        } else {
            ProsumerProfileOutcome.Failure(parseApiErrorMessage(response.errorBody()?.string(), response.code()))
        }
    }.getOrElse { error -> ProsumerProfileOutcome.Failure(error.toFriendlyMessage()) }

    suspend fun requestDeactivation(): ProsumerActionOutcome = runCatching {
        val response = apiService.requestMyProsumerDeactivation()
        if (response.isSuccessful) {
            ProsumerActionOutcome.Success
        } else {
            ProsumerActionOutcome.Failure(parseApiErrorMessage(response.errorBody()?.string(), response.code()))
        }
    }.getOrElse { error -> ProsumerActionOutcome.Failure(error.toFriendlyMessage()) }

    private fun Throwable.toFriendlyMessage(): String = when (this) {
        is IOException -> "Couldn't reach the server. Check your connection and try again."
        else -> "Something went wrong. Please try again."
    }
}
