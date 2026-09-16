package com.solgrid.mobile.core.network

import java.io.IOException

sealed interface ReservationListOutcome {
    data class Success(val response: PagedResponseDto<ReservationDto>) : ReservationListOutcome
    data class Failure(val message: String) : ReservationListOutcome
}

sealed interface ReservationOutcome {
    data class Success(val response: ReservationDto) : ReservationOutcome
    data class Failure(val message: String) : ReservationOutcome
}

sealed interface ReservationActionOutcome {
    data object Success : ReservationActionOutcome
    data class Failure(val message: String) : ReservationActionOutcome
}

sealed interface ReservationQrOutcome {
    data class Success(val response: ReservationQrResponseDto) : ReservationQrOutcome
    data class Failure(val message: String) : ReservationQrOutcome
}

sealed interface VerifyQrOutcome {
    data class Success(val response: VerifyReservationQrResponseDto) : VerifyQrOutcome
    data class Failure(val message: String) : VerifyQrOutcome
}

/** Wraps the Retrofit calls under api/v1/reservations for Prosumer and Grid Operator flows. */
class ReservationRepository(private val apiService: ApiService = NetworkModule.apiService) {

    suspend fun getById(id: String): ReservationOutcome = runCatching {
        val response = apiService.getReservation(id)
        val body = response.body()
        if (response.isSuccessful && body != null) {
            ReservationOutcome.Success(body)
        } else {
            ReservationOutcome.Failure(parseApiErrorMessage(response.errorBody()?.string(), response.code()))
        }
    }.getOrElse { error -> ReservationOutcome.Failure(error.toFriendlyMessage()) }

    suspend fun getMyReservations(status: Int? = null, pageNumber: Int = 1, pageSize: Int = 50): ReservationListOutcome =
        runCatching {
            val response = apiService.getMyReservations(status = status, pageNumber = pageNumber, pageSize = pageSize)
            val body = response.body()
            if (response.isSuccessful && body != null) {
                ReservationListOutcome.Success(body)
            } else {
                ReservationListOutcome.Failure(parseApiErrorMessage(response.errorBody()?.string(), response.code()))
            }
        }.getOrElse { error -> ReservationListOutcome.Failure(error.toFriendlyMessage()) }

    suspend fun getCurrent(pageNumber: Int = 1, pageSize: Int = 50): ReservationListOutcome = runCatching {
        val response = apiService.getCurrentReservations(pageNumber = pageNumber, pageSize = pageSize)
        val body = response.body()
        if (response.isSuccessful && body != null) {
            ReservationListOutcome.Success(body)
        } else {
            ReservationListOutcome.Failure(parseApiErrorMessage(response.errorBody()?.string(), response.code()))
        }
    }.getOrElse { error -> ReservationListOutcome.Failure(error.toFriendlyMessage()) }

    suspend fun create(
        prosumerId: String,
        stationId: String,
        bookingSlotId: String,
        scheduledAt: String,
    ): ReservationOutcome = runCatching {
        val response = apiService.createReservation(
            CreateReservationRequestDto(prosumerId = prosumerId, stationId = stationId, bookingSlotId = bookingSlotId, scheduledAt = scheduledAt),
        )
        val body = response.body()
        if (response.isSuccessful && body != null) {
            ReservationOutcome.Success(body)
        } else {
            ReservationOutcome.Failure(parseApiErrorMessage(response.errorBody()?.string(), response.code()))
        }
    }.getOrElse { error -> ReservationOutcome.Failure(error.toFriendlyMessage()) }

    suspend fun update(
        id: String,
        stationId: String,
        bookingSlotId: String,
        scheduledAt: String,
    ): ReservationOutcome = runCatching {
        val response = apiService.updateReservation(
            id,
            UpdateReservationRequestDto(stationId = stationId, bookingSlotId = bookingSlotId, scheduledAt = scheduledAt),
        )
        val body = response.body()
        if (response.isSuccessful && body != null) {
            ReservationOutcome.Success(body)
        } else {
            ReservationOutcome.Failure(parseApiErrorMessage(response.errorBody()?.string(), response.code()))
        }
    }.getOrElse { error -> ReservationOutcome.Failure(error.toFriendlyMessage()) }

    suspend fun cancel(id: String): ReservationActionOutcome = runCatching {
        val response = apiService.cancelReservation(id)
        if (response.isSuccessful) {
            ReservationActionOutcome.Success
        } else {
            ReservationActionOutcome.Failure(parseApiErrorMessage(response.errorBody()?.string(), response.code()))
        }
    }.getOrElse { error -> ReservationActionOutcome.Failure(error.toFriendlyMessage()) }

    suspend fun issueQr(id: String): ReservationQrOutcome = runCatching {
        val response = apiService.issueReservationQr(id)
        val body = response.body()
        if (response.isSuccessful && body != null) {
            ReservationQrOutcome.Success(body)
        } else {
            ReservationQrOutcome.Failure(parseApiErrorMessage(response.errorBody()?.string(), response.code()))
        }
    }.getOrElse { error -> ReservationQrOutcome.Failure(error.toFriendlyMessage()) }

    suspend fun verifyQr(reservationId: String, verificationToken: String): VerifyQrOutcome = runCatching {
        val response = apiService.verifyReservationQr(
            VerifyReservationQrRequestDto(reservationId = reservationId, verificationToken = verificationToken),
        )
        val body = response.body()
        if (response.isSuccessful && body != null) {
            VerifyQrOutcome.Success(body)
        } else {
            VerifyQrOutcome.Failure(parseApiErrorMessage(response.errorBody()?.string(), response.code()))
        }
    }.getOrElse { error -> VerifyQrOutcome.Failure(error.toFriendlyMessage()) }

    suspend fun complete(reservationId: String, verificationToken: String): ReservationOutcome = runCatching {
        val response = apiService.completeReservation(reservationId, CompleteReservationRequestDto(verificationToken))
        val body = response.body()
        if (response.isSuccessful && body != null) {
            ReservationOutcome.Success(body)
        } else {
            ReservationOutcome.Failure(parseApiErrorMessage(response.errorBody()?.string(), response.code()))
        }
    }.getOrElse { error -> ReservationOutcome.Failure(error.toFriendlyMessage()) }

    private fun Throwable.toFriendlyMessage(): String = when (this) {
        is IOException -> "Couldn't reach the server. Check your connection and try again."
        else -> "Something went wrong. Please try again."
    }
}
