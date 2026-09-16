import type { AccountStatus, UserRole } from '@/auth/types'

export interface User {
  id: string
  firstName: string
  lastName: string
  email: string
  role: UserRole
  status: AccountStatus
  createdAt: string
  updatedAt: string
}

/** Raw shape returned by the API (enums as numbers). */
export interface UserDto {
  id: string
  firstName: string
  lastName: string
  email: string
  role: number
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

export interface UserFilters {
  searchText: string
  role: UserRole | ''
  status: AccountStatus | ''
  pageNumber: number
  pageSize: number
}

export const defaultUserFilters: UserFilters = {
  searchText: '',
  role: '',
  status: '',
  pageNumber: 1,
  pageSize: 10,
}

export interface CreateUserInput {
  firstName: string
  lastName: string
  email: string
  password: string
  role: UserRole
}

export interface UpdateUserInput {
  firstName: string
  lastName: string
  email: string
  role: UserRole
}
