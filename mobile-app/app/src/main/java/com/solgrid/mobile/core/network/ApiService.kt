package com.solgrid.mobile.core.network

import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.PATCH
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query

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

    @GET("api/v1/stations")
    suspend fun getStations(
        @Query("status") status: Int? = null,
        @Query("searchText") searchText: String? = null,
        @Query("hasAvailableSlots") hasAvailableSlots: Boolean? = null,
        @Query("pageNumber") pageNumber: Int = 1,
        @Query("pageSize") pageSize: Int = 20,
    ): Response<PagedResponseDto<SolarStationDto>>

    @GET("api/v1/stations/nearby")
    suspend fun getNearbyStations(
        @Query("latitude") latitude: Double,
        @Query("longitude") longitude: Double,
        @Query("radiusKilometers") radiusKilometers: Double = 10.0,
        @Query("maxResults") maxResults: Int = 20,
        @Query("activeOnly") activeOnly: Boolean = true,
    ): Response<List<SolarStationDto>>

    @GET("api/v1/stations/{id}")
    suspend fun getStation(@Path("id") id: String): Response<SolarStationDto>

    @GET("api/v1/stations/{stationId}/slots")
    suspend fun getStationSlots(
        @Path("stationId") stationId: String,
        @Query("status") status: Int? = null,
        @Query("from") from: String? = null,
        @Query("to") to: String? = null,
        @Query("pageNumber") pageNumber: Int = 1,
        @Query("pageSize") pageSize: Int = 20,
    ): Response<PagedResponseDto<BookingSlotDto>>

    @GET("api/v1/reservations")
    suspend fun getStationReservations(
        @Query("stationId") stationId: String,
        @Query("status") status: Int? = null,
        @Query("searchText") searchText: String? = null,
        @Query("pageNumber") pageNumber: Int = 1,
        @Query("pageSize") pageSize: Int = 20,
    ): Response<PagedResponseDto<ReservationDto>>
}
