import { filterByRole } from '@/auth/roleAccess'
import type { RoleRestricted } from '@/auth/roleAccess'
import type { UserRole } from '@/auth/types'

export interface QuickAction extends RoleRestricted {
  id: string
  to: string
  label: string
  description: string
}

/** Every `to` must stay reachable for the roles listed, or the action becomes a link to /403. */
export const quickActions: QuickAction[] = [
  {
    id: 'open-analytics',
    to: '/analytics',
    label: 'Open trading analytics',
    description: 'Review every node, reservation, and business rule.',
  },
  {
    id: 'review-reservations',
    to: '/reservations',
    label: 'Review pending reservations',
    description: 'Approve or reject prosumer booking requests.',
    allowedRoles: ['Backoffice', 'GridOperator'],
  },
  {
    id: 'slot-availability',
    to: '/microgrid',
    label: 'Update slot availability',
    description: 'Open a node and activate or deactivate its battery slots.',
  },
  {
    id: 'create-node',
    to: '/microgrid/new',
    label: 'Add a microgrid node',
    description: 'Register a new solar station and its weekly operating hours.',
    allowedRoles: ['Backoffice'],
  },
  {
    id: 'manage-users',
    to: '/users',
    label: 'Manage web users',
    description: 'Create or deactivate Backoffice and Grid Operator accounts.',
    allowedRoles: ['Backoffice'],
  },
  {
    id: 'review-prosumers',
    to: '/prosumers',
    label: 'Review prosumer sign-ups',
    description: 'Approve new EV owner registrations and deactivation requests.',
    allowedRoles: ['Backoffice'],
  },
]

export function visibleQuickActions(role: UserRole | undefined): QuickAction[] {
  return filterByRole(quickActions, role)
}

export const roleIntro: Record<UserRole, string> = {
  Backoffice:
    'You have full console access: web users, prosumer accounts, microgrid nodes and reservations.',
  GridOperator:
    'You can review reservations and keep microgrid node slot availability up to date.',
}
