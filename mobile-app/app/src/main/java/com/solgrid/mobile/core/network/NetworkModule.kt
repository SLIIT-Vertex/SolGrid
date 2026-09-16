package com.solgrid.mobile.core.network

import kotlinx.serialization.json.Json
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.kotlinx.serialization.asConverterFactory
import java.util.concurrent.TimeUnit

/** Backend URL is configured with Gradle's API_BASE_URL property and must end with /.
 * The physical-device default targets the current LAN host; use 10.0.2.2 for an emulator.
 */
private const val BASE_URL = com.solgrid.mobile.BuildConfig.API_BASE_URL

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

    /** A 401 on a request that carried our bearer token means the session expired or was
     * revoked server-side — distinct from a 401 on an anonymous login attempt (wrong password),
     * which never has this header. Clears the stale session and signals the nav layer. */
    private val sessionExpiryInterceptor = okhttp3.Interceptor { chain ->
        val request = chain.request()
        val response = chain.proceed(request)
        if (response.code == 401 && request.header("Authorization") != null) {
            SessionStore.clear()
            SessionExpiryNotifier.notifyExpired()
        }
        response
    }

    private val okHttpClient = OkHttpClient.Builder()
        .addInterceptor(authInterceptor)
        .addInterceptor(sessionExpiryInterceptor)
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
