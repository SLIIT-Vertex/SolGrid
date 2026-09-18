import type { UserRole } from '@/auth/types'

/** Anything the UI offers conditionally: undefined `allowedRoles` means every signed-in role. */
export interface RoleRestricted {
  allowedRoles?: UserRole[]
}

export function allowsRole(item: RoleRestricted, role: UserRole): boolean {
  return !item.allowedRoles || item.allowedRoles.includes(role)
}

export function filterByRole<T extends RoleRestricted>(items: T[], role: UserRole | undefined): T[] {
  if (!role) return []
  return items.filter((item) => allowsRole(item, role))
}
