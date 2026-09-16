export type UserRole = 'Backoffice' | 'GridOperator'

export type AccountStatus = 'Active' | 'Inactive'

export const UserRoleValue = {
  Backoffice: 1,
  GridOperator: 2,
} as const satisfies Record<UserRole, number>

export const AccountStatusValue = {
  Active: 1,
  Inactive: 2,
} as const satisfies Record<AccountStatus, number>

export function userRoleFromValue(value: number): UserRole {
  return value === UserRoleValue.Backoffice ? 'Backoffice' : 'GridOperator'
}

export function accountStatusFromValue(value: number): AccountStatus {
  return value === AccountStatusValue.Active ? 'Active' : 'Inactive'
}

export interface LoginRequest {
  email: string
  password: string
}

/** Raw shape returned by POST /api/v1/auth/login (enums as numbers). */
export interface LoginResponseDto {
  accessToken: string
  expiresAt: string
  user: {
    id: string
    firstName: string
    lastName: string
    email: string
    role: number
    status: number
    createdAt: string
    updatedAt: string
  }
}

export interface CurrentIdentity {
  userId: string
  role: UserRole
}

export interface AuthSession {
  token: string
  expiresAt: string
  userId: string
  firstName: string
  lastName: string
  email: string
  role: UserRole
}
