import { describe, expect, it } from 'vitest'
import { canChangeRole, canDeactivateUser, isCurrentUser } from '@/features/users/permissions'

describe('user permissions', () => {
  it('identifies the signed-in account by id', () => {
    expect(isCurrentUser('user-1', { id: 'user-1' })).toBe(true)
    expect(isCurrentUser('user-1', { id: 'user-2' })).toBe(false)
  })

  it('treats a missing current user id as not matching anyone', () => {
    expect(isCurrentUser(undefined, { id: 'user-1' })).toBe(false)
  })

  it('blocks deactivating your own account only', () => {
    expect(canDeactivateUser('user-1', { id: 'user-1' })).toBe(false)
    expect(canDeactivateUser('user-1', { id: 'user-2' })).toBe(true)
    expect(canDeactivateUser(undefined, { id: 'user-1' })).toBe(true)
  })

  it('blocks changing your own role only', () => {
    expect(canChangeRole('user-1', { id: 'user-1' })).toBe(false)
    expect(canChangeRole('user-1', { id: 'user-2' })).toBe(true)
    expect(canChangeRole(undefined, { id: 'user-1' })).toBe(true)
  })
})
