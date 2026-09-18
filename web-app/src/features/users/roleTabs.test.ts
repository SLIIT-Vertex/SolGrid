import { describe, expect, it } from 'vitest'
import { findUserRoleTab, roleForTabId, tabIdForRole, userRoleTabs } from '@/features/users/roleTabs'

describe('user role tabs', () => {
  it('maps the "all" tab to an empty role filter and back', () => {
    expect(roleForTabId('all')).toBe('')
    expect(tabIdForRole('')).toBe('all')
  })

  it('maps each role tab to its own role filter and back', () => {
    expect(roleForTabId('Backoffice')).toBe('Backoffice')
    expect(roleForTabId('GridOperator')).toBe('GridOperator')
    expect(tabIdForRole('Backoffice')).toBe('Backoffice')
    expect(tabIdForRole('GridOperator')).toBe('GridOperator')
  })

  it('exposes one tab per role plus an "all" tab', () => {
    expect(userRoleTabs.map((tab) => tab.id)).toEqual(['all', 'Backoffice', 'GridOperator'])
  })

  it('returns a tab with an empty-state description for every id', () => {
    for (const tab of userRoleTabs) {
      expect(findUserRoleTab(tab.id).emptyDescription).not.toBe('')
    }
  })
})
