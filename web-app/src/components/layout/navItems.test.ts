import { describe, expect, it } from 'vitest'
import { visibleNavItems } from '@/components/layout/navItems'

describe('visibleNavItems', () => {
  it('gives Backoffice the full console', () => {
    const ids = visibleNavItems('Backoffice').map((item) => item.id)
    expect(ids).toEqual(['dashboard', 'analytics', 'reservations', 'microgrid', 'users', 'prosumers'])
  })

  it('hides Backoffice-only sections from Grid Operators', () => {
    const ids = visibleNavItems('GridOperator').map((item) => item.id)
    expect(ids).toEqual(['dashboard', 'analytics', 'reservations', 'microgrid'])
    expect(ids).not.toContain('users')
    expect(ids).not.toContain('prosumers')
  })

  it('shows nothing without a session', () => {
    expect(visibleNavItems(undefined)).toEqual([])
  })
})
