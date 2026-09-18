import type { UserRole } from '@/auth/types'

export type UserRoleTabId = 'all' | UserRole

export interface UserRoleTab {
  id: UserRoleTabId
  label: string
  /** Role filter sent to the API. An empty string loads every role. */
  role: UserRole | ''
  /** Shown by the empty state when this tab has no matching users. */
  emptyDescription: string
}

export const userRoleTabs: UserRoleTab[] = [
  {
    id: 'all',
    label: 'All users',
    role: '',
    emptyDescription: 'Try adjusting your search or status filter, or create a new account.',
  },
  {
    id: 'Backoffice',
    label: 'Backoffice',
    role: 'Backoffice',
    emptyDescription: 'No Backoffice accounts match these filters. Create one to grant full console access.',
  },
  {
    id: 'GridOperator',
    label: 'Grid Operators',
    role: 'GridOperator',
    emptyDescription:
      'No Grid Operator accounts match these filters. Create one to let staff manage reservations and slot availability.',
  },
]

export function tabIdForRole(role: UserRole | ''): UserRoleTabId {
  return role === '' ? 'all' : role
}

export function roleForTabId(id: UserRoleTabId): UserRole | '' {
  return id === 'all' ? '' : id
}

export function findUserRoleTab(id: UserRoleTabId): UserRoleTab {
  return userRoleTabs.find((tab) => tab.id === id) ?? userRoleTabs[0]
}
