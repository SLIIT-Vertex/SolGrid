import type { UserRole } from '@/auth/types'

export function getReservationPermissions(role: UserRole | undefined) {
  const backoffice = role === 'Backoffice'
  return {
    canManageBookings: backoffice,
    canApprove: backoffice,
    canReject: backoffice,
    canReadProsumers: backoffice,
  }
}
