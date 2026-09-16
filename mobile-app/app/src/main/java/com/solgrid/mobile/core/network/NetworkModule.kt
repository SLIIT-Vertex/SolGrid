package com.solgrid.mobile.core.network

import kotlinx.serialization.json.Json
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.kotlinx.serialization.asConverterFactory
import java.util.concurrent.TimeUnit

/**
 * 10.0.2.2 is the Android emulator's alias for the host machine's localhost, where the SolGrid
 * backend runs on port 5080 (see web-service/.env BACKEND_PORT). Point this at a LAN IP instead
 * when running on a physical device.
 */
private const val BASE_URL = "http://10.0.2.2:5080/"

object NetworkModule {
    private val json = Json { ignoreUnknownKeys = true }

    private val loggingInterceptor = HttpLoggingInterceptor().apply {
        // Never log authorization headers, request bodies, or response bodies containing account data.
        level = HttpLoggingInterceptor.Level.BASIC
    }

    /** Attaches the current session's bearer token, if any, to every outgoing request. */
    private val authInterceptor = okhttp3.Interceptor { chain ->
        val token = SessionStore.accessToken
        val request = if (token != null) {
            chain.request().newBuilder().addHeader("Authorization", "Bearer $token").build()
        } else {
            chain.request()
        }
        chain.proceed(request)
    }

    private val okHttpClient = OkHttpClient.Builder()
        .addInterceptor(authInterceptor)
        .addInterceptor(loggingInterceptor)
        .connectTimeout(15, TimeUnit.SECONDS)
        .readTimeout(15, TimeUnit.SECONDS)
        .build()

    val apiService: ApiService = Retrofit.Builder()
        .baseUrl(BASE_URL)
        .client(okHttpClient)
        .addConverterFactory(json.asConverterFactory("application/json".toMediaType()))
        .build()
        .create(ApiService::class.java)
}
