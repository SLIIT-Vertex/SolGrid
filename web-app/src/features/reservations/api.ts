import { apiClient } from '@/lib/apiClient'
import { toProsumer } from '@/features/prosumers/api'
import { reservationStatusFromValue, ReservationStatusValue } from '@/features/reservations/types'
import type {
  PagedResultDto,
  Reservation,
  ReservationDashboardSummaryDto,
  ReservationDashboardTab,
  ReservationDto,
  ReservationFilters,
  RejectReservationInput,
} from '@/features/reservations/types'

function toReservation(dto: ReservationDto): Reservation {
  return {
    id: dto.id,
    referenceCode: dto.referenceCode,
    prosumerId: dto.prosumerId,
    prosumerDetails: dto.prosumerDetails ? toProsumer(dto.prosumerDetails) : null,
    stationId: dto.stationId,
    bookingSlotId: dto.bookingSlotId,
    scheduledAt: dto.scheduledAt,
    status: reservationStatusFromValue(dto.status),
    createdAt: dto.createdAt,
    updatedAt: dto.updatedAt,
    approvedAt: dto.approvedAt,
    approvedBy: dto.approvedBy,
    rejectedAt: dto.rejectedAt,
    rejectedBy: dto.rejectedBy,
    rejectionReason: dto.rejectionReason,
    cancelledAt: dto.cancelledAt,
    completedAt: dto.completedAt,
    completedBy: dto.completedBy,
    hasQrVerificationToken: dto.hasQrVerificationToken,
    qrVerificationTokenIssuedAt: dto.qrVerificationTokenIssuedAt,
    qrVerificationTokenExpiresAt: dto.qrVerificationTokenExpiresAt,
    qrVerifiedAt: dto.qrVerifiedAt,
  }
}

export interface ReservationPage {
  items: Reservation[]
  totalCount: number
  pageNumber: number
  pageSize: number
}

function buildParams(filters: ReservationFilters) {
  return {
    searchText: filters.searchText.trim() || undefined,
    prosumerId: filters.prosumerId.trim() || undefined,
    stationId: filters.stationId.trim() || undefined,
    status: filters.status ? ReservationStatusValue[filters.status] : undefined,
    scheduledFrom: filters.scheduledFrom ? `${filters.scheduledFrom}T00:00:00+00:00` : undefined,
    scheduledTo: filters.scheduledTo ? `${filters.scheduledTo}T23:59:59+00:00` : undefined,
    pageNumber: filters.pageNumber,
    pageSize: filters.pageSize,
  }
}

export async function getReservations(filters: ReservationFilters): Promise<ReservationPage> {
  const { data } = await apiClient.get<PagedResultDto<ReservationDto>>('/api/v1/reservations', {
    params: buildParams(filters),
  })

  return {
    items: data.items.map(toReservation),
    totalCount: data.totalCount,
    pageNumber: data.pageNumber,
    pageSize: data.pageSize,
  }
}

const dashboardTabRoute: Record<ReservationDashboardTab, string> = {
  current: '/api/v1/reservations/current',
  pending: '/api/v1/reservations/pending',
  history: '/api/v1/reservations/history',
}

export async function getDashboardReservations(
  tab: ReservationDashboardTab,
  filters: ReservationFilters,
): Promise<ReservationPage> {
  const { data } = await apiClient.get<PagedResultDto<ReservationDto>>(dashboardTabRoute[tab], {
    params: buildParams(filters),
  })

  return {
    items: data.items.map(toReservation),
    totalCount: data.totalCount,
    pageNumber: data.pageNumber,
    pageSize: data.pageSize,
  }
}

export async function getDashboardSummary(): Promise<ReservationDashboardSummaryDto> {
  const { data } = await apiClient.get<ReservationDashboardSummaryDto>('/api/v1/reservations/dashboard/summary')
  return data
}

export async function getReservation(id: string): Promise<Reservation> {
  const { data } = await apiClient.get<ReservationDto>(`/api/v1/reservations/${id}`)
  return toReservation(data)
}

export async function approveReservation(id: string): Promise<Reservation> {
  const { data } = await apiClient.patch<ReservationDto>(`/api/v1/reservations/${id}/approve`, {})
  return toReservation(data)
}

export async function rejectReservation(id: string, input: RejectReservationInput): Promise<Reservation> {
  const { data } = await apiClient.patch<ReservationDto>(`/api/v1/reservations/${id}/reject`, input)
  return toReservation(data)
}

export async function cancelReservation(id: string): Promise<void> {
  await apiClient.patch(`/api/v1/reservations/${id}/cancel`)
}

export interface ReservationScheduleRequest { stationId: string; bookingSlotId: string; scheduledAt: string }
export async function createReservation(request: ReservationScheduleRequest & { prosumerId: string }): Promise<Reservation> {
  const { data } = await apiClient.post<ReservationDto>('/api/v1/reservations', request)
  return toReservation(data)
}
export async function updateReservation(id: string, request: ReservationScheduleRequest): Promise<Reservation> {
  const { data } = await apiClient.put<ReservationDto>(`/api/v1/reservations/${id}`, request)
  return toReservation(data)
}
