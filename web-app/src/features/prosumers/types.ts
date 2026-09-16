export type ProsumerAccountStatus = 'Pending' | 'Active' | 'DeactivationRequested' | 'Deactivated'

export const ProsumerAccountStatusValue = {
  Pending: 1,
  Active: 2,
  DeactivationRequested: 3,
  Deactivated: 4,
} as const satisfies Record<ProsumerAccountStatus, number>

const statusByValue: Record<number, ProsumerAccountStatus> = {
  1: 'Pending',
  2: 'Active',
  3: 'DeactivationRequested',
  4: 'Deactivated',
}

export function prosumerStatusFromValue(value: number): ProsumerAccountStatus {
  return statusByValue[value] ?? 'Pending'
}

export interface Prosumer {
  nic: string
  firstName: string
  lastName: string
  email: string
  phoneNumber: string | null
  status: ProsumerAccountStatus
  createdAt: string
  updatedAt: string
}

/** Raw shape returned by the API (status as a number). */
export interface ProsumerDto {
  nic: string
  firstName: string
  lastName: string
  email: string
  phoneNumber: string | null
  status: number
  createdAt: string
  updatedAt: string
}

export interface PagedResultDto<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
}

export interface ProsumerFilters {
  searchText: string
  status: ProsumerAccountStatus | ''
  pageNumber: number
  pageSize: number
}

export const defaultProsumerFilters: ProsumerFilters = {
  searchText: '',
  status: '',
  pageNumber: 1,
  pageSize: 10,
}

export const defaultPendingProsumerFilters: ProsumerFilters = {
  searchText: '',
  status: 'Pending',
  pageNumber: 1,
  pageSize: 10,
}
