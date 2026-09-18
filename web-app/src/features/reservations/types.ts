import type { Prosumer, ProsumerDto } from '@/features/prosumers/types'

export type ReservationAction = 'approve' | 'reject' | 'cancel'

export interface PendingReservationAction {
  reservation: Reservation
  action: ReservationAction
}

export type ReservationStatus =
  'Pending' | 'Approved' | 'Rejected' | 'Cancelled' | 'Completed'

export const ReservationStatusValue = {
  Pending: 1,
  Approved: 2,
  Rejected: 3,
  Cancelled: 4,
  Completed: 5,
} as const satisfies Record<ReservationStatus, number>

const statusByValue: Record<number, ReservationStatus> = {
  1: 'Pending',
  2: 'Approved',
  3: 'Rejected',
  4: 'Cancelled',
  5: 'Completed',
}

export function reservationStatusFromValue(value: number): ReservationStatus {
  return statusByValue[value] ?? 'Pending'
}

export type ReservationDashboardTab = 'current' | 'pending' | 'history'

export interface Reservation {
  prosumerDetails?: Prosumer | null
  id: string
  referenceCode: string
  prosumerId: string
  stationId: string
  bookingSlotId: string
  scheduledAt: string
  status: ReservationStatus
  createdAt: string
  updatedAt: string
  approvedAt: string | null
  approvedBy: string | null
  rejectedAt: string | null
  rejectedBy: string | null
  rejectionReason: string | null
  cancelledAt: string | null
  completedAt: string | null
  completedBy: string | null
  hasQrVerificationToken: boolean
  qrVerificationTokenIssuedAt: string | null
  qrVerificationTokenExpiresAt: string | null
  qrVerifiedAt: string | null
}

/** Raw shape from the API (status as a number). */
export interface ReservationDto {
  prosumerDetails?: ProsumerDto | null
  id: string
  referenceCode: string
  prosumerId: string
  stationId: string
  bookingSlotId: string
  scheduledAt: string
  status: number
  createdAt: string
  updatedAt: string
  approvedAt: string | null
  approvedBy: string | null
  rejectedAt: string | null
  rejectedBy: string | null
  rejectionReason: string | null
  cancelledAt: string | null
  completedAt: string | null
  completedBy: string | null
  hasQrVerificationToken: boolean
  qrVerificationTokenIssuedAt: string | null
  qrVerificationTokenExpiresAt: string | null
  qrVerifiedAt: string | null
}

export interface PagedResultDto<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
}

export interface ReservationDashboardSummaryDto {
  pendingReservationsCount: number
  approvedFutureReservationsCount: number
  currentReservationsCount: number
  bookingHistoryCount: number
}

export interface ReservationFilters {
  searchText: string
  prosumerId: string
  stationId: string
  status: ReservationStatus | ''
  scheduledFrom: string
  scheduledTo: string
  pageNumber: number
  pageSize: number
}

export const defaultReservationFilters: ReservationFilters = {
  searchText: '',
  prosumerId: '',
  stationId: '',
  status: '',
  scheduledFrom: '',
  scheduledTo: '',
  pageNumber: 1,
  pageSize: 5,
}

export interface RejectReservationInput {
  rejectionReason: string
}
