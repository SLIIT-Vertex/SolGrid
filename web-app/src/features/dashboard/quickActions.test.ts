import { describe, expect, it } from 'vitest'
import { navItems } from '@/components/layout/navItems'
import { roleIntro, visibleQuickActions } from '@/features/dashboard/quickActions'
import { allowsRole } from '@/auth/roleAccess'
import type { UserRole } from '@/auth/types'

const roles: UserRole[] = ['Backoffice', 'GridOperator']

describe('dashboard quick actions', () => {
  it('offers Backoffice the administrative shortcuts', () => {
    const ids = visibleQuickActions('Backoffice').map((action) => action.id)
    expect(ids).toContain('manage-users')
    expect(ids).toContain('review-prosumers')
    expect(ids).toContain('create-node')
  })

  it('hides Backoffice-only shortcuts from Grid Operators', () => {
    const ids = visibleQuickActions('GridOperator').map((action) => action.id)
    expect(ids).toEqual(['review-reservations', 'slot-availability'])
  })

  it('shows nothing without a session', () => {
    expect(visibleQuickActions(undefined)).toEqual([])
  })

  it('never points a role at a section its nav hides', () => {
    for (const role of roles) {
      const reachable = navItems.filter((item) => allowsRole(item, role)).map((item) => item.to)
      for (const action of visibleQuickActions(role)) {
        const section = reachable.find(
          (to) => action.to === to || (to !== '/' && action.to.startsWith(`${to}/`)),
        )
        expect(section, `${action.id} is unreachable for ${role}`).toBeDefined()
      }
    }
  })

  it('introduces every role', () => {
    for (const role of roles) {
      expect(roleIntro[role]).not.toBe('')
    }
  })
})
