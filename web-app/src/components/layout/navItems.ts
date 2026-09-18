import { filterByRole } from '@/auth/roleAccess'
import type { RoleRestricted } from '@/auth/roleAccess'
import type { UserRole } from '@/auth/types'

export type NavItemId = 'dashboard' | 'reservations' | 'microgrid' | 'users' | 'prosumers'

export interface NavItem extends RoleRestricted {
  id: NavItemId
  to: string
  label: string
}

/** Mirrors the route guards in `@/routes/router`, so the nav never offers a link that ends in /403. */
export const navItems: NavItem[] = [
  { id: 'dashboard', to: '/', label: 'Dashboard' },
  { id: 'reservations', to: '/reservations', label: 'Reservations', allowedRoles: ['Backoffice', 'GridOperator'] },
  { id: 'microgrid', to: '/microgrid', label: 'Microgrid Nodes' },
  { id: 'users', to: '/users', label: 'Web Users', allowedRoles: ['Backoffice'] },
  { id: 'prosumers', to: '/prosumers', label: 'Prosumers', allowedRoles: ['Backoffice'] },
]

export function visibleNavItems(role: UserRole | undefined): NavItem[] {
  return filterByRole(navItems, role)
}
