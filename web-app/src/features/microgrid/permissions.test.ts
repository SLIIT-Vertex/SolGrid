import { describe, expect, it } from 'vitest'
import type { UserRole } from '@/auth/types'
import {
  canChangeNodeStatus,
  canCreateNode,
  canEditNode,
  canManageSchedule,
  canManageSlots,
} from '@/features/microgrid/permissions'

function user(role: UserRole) {
  return { role }
}

describe('microgrid permissions', () => {
  it('grants Backoffice administrative access', () => {
    const backoffice = user('Backoffice')

    expect(canCreateNode(backoffice)).toBe(true)
    expect(canEditNode(backoffice)).toBe(true)
    expect(canManageSchedule(backoffice)).toBe(true)
    expect(canManageSlots(backoffice)).toBe(true)
    expect(canChangeNodeStatus(backoffice)).toBe(true)
  })

  it('keeps Grid Operator read-only', () => {
    const operator = user('GridOperator')

    expect(canCreateNode(operator)).toBe(false)
    expect(canEditNode(operator)).toBe(false)
    expect(canManageSchedule(operator)).toBe(false)
    expect(canManageSlots(operator)).toBe(false)
    expect(canChangeNodeStatus(operator)).toBe(false)
  })

  it('denies access without a session', () => {
    expect(canCreateNode(null)).toBe(false)
    expect(canEditNode(undefined)).toBe(false)
    expect(canManageSchedule(null)).toBe(false)
    expect(canManageSlots(undefined)).toBe(false)
    expect(canChangeNodeStatus(null)).toBe(false)
  })
})
