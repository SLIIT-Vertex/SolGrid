package com.solgrid.mobile.core.network

import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.PATCH
import retrofit2.http.POST
import retrofit2.http.PUT

interface ApiService {
    @POST("api/v1/auth/login")
    suspend fun login(@Body request: LoginRequestDto): Response<LoginResponseDto>

    @POST("api/v1/prosumers/register")
    suspend fun registerProsumer(@Body request: RegisterProsumerRequestDto): Response<ProsumerResponseDto>

    @POST("api/v1/prosumers/login")
    suspend fun loginProsumer(@Body request: LoginRequestDto): Response<ProsumerLoginResponseDto>

    @GET("api/v1/prosumers/me")
    suspend fun getMyProsumerProfile(): Response<ProsumerResponseDto>

    @PUT("api/v1/prosumers/me")
    suspend fun updateMyProsumerProfile(@Body request: UpdateProsumerRequestDto): Response<ProsumerResponseDto>

    @PATCH("api/v1/prosumers/me/request-deactivation")
    suspend fun requestMyProsumerDeactivation(): Response<Unit>
}
