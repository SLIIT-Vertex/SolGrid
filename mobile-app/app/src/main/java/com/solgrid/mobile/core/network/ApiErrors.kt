package com.solgrid.mobile.core.network

import kotlinx.serialization.json.Json

private val errorJson = Json { ignoreUnknownKeys = true }

/** Maps a failed response's Problem Details body + status code to a user-facing message. */
fun parseApiErrorMessage(errorBody: String?, statusCode: Int): String {
    val problem = errorBody?.let {
        runCatching { errorJson.decodeFromString<ProblemDetailsDto>(it) }.getOrNull()
    }
    return when (statusCode) {
        401 -> "Incorrect credentials. Please try again."
        403 -> problem?.title ?: "This account is inactive. Contact an administrator."
        404 -> problem?.title ?: "Not found."
        409 -> problem?.title ?: "That value is already in use."
        else -> problem?.title ?: "Something went wrong. Please try again."
    }
}
