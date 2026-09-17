import { describe, expect, it } from 'vitest'
import {
  sanitizeNicInput,
  sanitizePhoneInput,
  validatePhoneNumber,
  validateSriLankanNic,
} from '@/features/prosumers/validation'

describe('prosumer input constraints', () => {
  it('keeps modern NIC values numeric and capped at 12 digits', () => {
    expect(sanitizeNicInput('2001-ab1470123456')).toBe('200114701234')
  })

  it('supports the legacy NIC suffix but rejects other letters', () => {
    expect(sanitizeNicInput('123456789vabc')).toBe('123456789V')
    expect(validateSriLankanNic('123456789V')).toBeUndefined()
  })

  it('keeps only the first ten phone digits', () => {
    expect(sanitizePhoneInput('077-call-123456789')).toBe('0771234567')
  })

  it('enforces the phone digit count', () => {
    expect(validatePhoneNumber('771234567')).toBe(
      'Enter a 10-digit Sri Lankan phone number beginning with 0',
    )
    expect(validatePhoneNumber('0771234567')).toBeUndefined()
  })
})
