import { describe, expect, it } from 'vitest'
import {
  normalizeUserFormValues,
  validateEmail,
  validateName,
  validatePassword,
} from '@/features/users/validation'

describe('user form validation', () => {
  it('normalizes profile text without changing the password', () => {
    expect(
      normalizeUserFormValues({
        firstName: '  Jane ',
        lastName: ' Doe  ',
        email: ' JANE.DOE@EXAMPLE.COM ',
        password: ' password ',
      }),
    ).toEqual({
      firstName: 'Jane',
      lastName: 'Doe',
      email: 'jane.doe@example.com',
      password: ' password ',
    })
  })

  it('rejects names containing only whitespace', () => {
    expect(validateName('   ', 'First name')).toBe('First name is required')
  })

  it('rejects malformed email addresses', () => {
    expect(validateEmail('person@example')).toBe('Enter a valid email address')
  })

  it('enforces the password length range', () => {
    expect(validatePassword('short')).toBe('Password must be at least 8 characters')
    expect(validatePassword('valid-password')).toBe(true)
  })
})
