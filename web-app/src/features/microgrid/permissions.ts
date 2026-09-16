import type { UserRole } from '@/auth/types'

type RoleHolder = { role: UserRole } | null | undefined

function isBackoffice(user: RoleHolder): boolean {
  return user?.role === 'Backoffice'
}

export function canCreateNode(user: RoleHolder): boolean {
  return isBackoffice(user)
}

export function canEditNode(user: RoleHolder): boolean {
  return isBackoffice(user)
}

export function canManageSchedule(user: RoleHolder): boolean {
  return isBackoffice(user)
}

export function canManageSlots(user: RoleHolder): boolean {
  return isBackoffice(user)
}

export function canChangeNodeStatus(user: RoleHolder): boolean {
  return isBackoffice(user)
}
