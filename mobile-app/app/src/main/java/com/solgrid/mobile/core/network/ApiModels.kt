package com.solgrid.mobile.core.network

import kotlinx.serialization.Serializable

/**
 * Wire-format DTOs mirroring the backend's SolGrid.Application.Auth request/response contracts
 * (POST /api/v1/auth/login). ASP.NET Core's default System.Text.Json settings serialize to
 * camelCase, so field names here follow that casing rather than the C# PascalCase property names.
 */
@Serializable
data class LoginRequestDto(
    val email: String,
    val password: String
)

@Serializable
data class LoginResponseDto(
    val accessToken: String,
    val expiresAt: String,
    val user: UserResponseDto
)

/**
 * `role`/`status` arrive as raw enum ordinals (ASP.NET's default System.Text.Json serialization
 * for enums), not strings — see SolGrid.Domain.Enums.UserRole (Backoffice = 1, GridOperator = 2)
 * and AccountStatus.
 */
@Serializable
data class UserResponseDto(
    val id: String,
    val firstName: String,
    val lastName: String,
    val email: String,
    val role: Int,
    val status: Int,
    val createdAt: String,
    val updatedAt: String
)

@Serializable
data class ProblemDetailsDto(
    val title: String? = null,
    val detail: String? = null,
    val status: Int? = null
)
