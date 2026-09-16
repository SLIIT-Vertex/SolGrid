package com.solgrid.mobile.core.network

import kotlinx.serialization.json.Json
import java.io.IOException

sealed interface LoginOutcome {
    data class Success(val response: LoginResponseDto) : LoginOutcome
    data class Failure(val message: String) : LoginOutcome
}

/**
 * Wraps the raw Retrofit call for POST /api/v1/auth/login, translating HTTP/network failures into
 * user-facing messages. Only Grid Operator sign-in uses this today — Prosumer login/registration
 * remain UI-mocked pending their own backend integration.
 */
class AuthRepository(private val apiService: ApiService = NetworkModule.apiService) {

    private val json = Json { ignoreUnknownKeys = true }

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
                LoginOutcome.Failure(parseErrorMessage(response.errorBody()?.string(), response.code()))
            }
        } catch (error: IOException) {
            LoginOutcome.Failure("Couldn't reach the server. Check your connection and try again.")
        } catch (error: Exception) {
            LoginOutcome.Failure("Something went wrong. Please try again.")
        }
    }

    private fun parseErrorMessage(errorBody: String?, statusCode: Int): String {
        val problem = errorBody?.let {
            runCatching { json.decodeFromString<ProblemDetailsDto>(it) }.getOrNull()
        }
        return when (statusCode) {
            401 -> "Incorrect credentials. Please try again."
            403 -> problem?.title ?: "This account is inactive. Contact your administrator."
            else -> problem?.title ?: "Something went wrong. Please try again."
        }
    }
}
