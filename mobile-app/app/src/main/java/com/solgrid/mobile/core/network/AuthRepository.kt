package com.solgrid.mobile.core.network

import java.io.IOException

sealed interface LoginOutcome {
    data class Success(val response: LoginResponseDto) : LoginOutcome
    data class Failure(val message: String, val statusCode: Int? = null) : LoginOutcome
}

/** Wraps the raw Retrofit call for POST /api/v1/auth/login (Backoffice/Grid Operator sign-in). */
class AuthRepository(private val apiService: ApiService = NetworkModule.apiService) {

    suspend fun login(email: String, password: String): LoginOutcome {
        return try {
            val response = apiService.login(LoginRequestDto(email = email, password = password))
            if (response.isSuccessful) {
                val body = response.body()
                if (body != null) {
                    LoginOutcome.Success(body)
                } else {
                    LoginOutcome.Failure("Unexpected empty response from server.")
                }
            } else {
                LoginOutcome.Failure(
                    parseApiErrorMessage(response.errorBody()?.string(), response.code()),
                    statusCode = response.code(),
                )
            }
        } catch (error: IOException) {
            LoginOutcome.Failure("Couldn't reach the server. Check your connection and try again.")
        } catch (error: Exception) {
            LoginOutcome.Failure("Something went wrong. Please try again.")
        }
    }
}
