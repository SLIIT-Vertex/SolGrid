import { describe, expect, it } from 'vitest'
import {
  sanitizeEmailInput,
  sanitizeNameInput,
  sanitizeNicInput,
  sanitizePhoneInput,
  validatePhoneNumber,
  validateProsumerEmail,
  validateProsumerForm,
  validateProsumerName,
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

  it('treats an omitted phone number as acceptable', () => {
    expect(validatePhoneNumber('')).toBeUndefined()
    expect(validatePhoneNumber('', true)).toBe('Phone number is required')
  })

  it('keeps names to letters, spaces, and common punctuation', () => {
    expect(sanitizeNameInput('  Nim4l   de   Silva!')).toBe('Niml de Silva')
    expect(validateProsumerName("O'Brien-Smith", 'First name')).toBeUndefined()
    expect(validateProsumerName('', 'First name')).toBe('First name is required')
  })

  it('strips whitespace from emails and checks their shape', () => {
    expect(sanitizeEmailInput(' nimal @solgrid.lk ')).toBe('nimal@solgrid.lk')
    expect(validateProsumerEmail('nimal@solgrid.lk')).toBeUndefined()
    expect(validateProsumerEmail('nimal@solgrid')).toBe('Enter a valid email address')
  })
})

describe('validateProsumerForm', () => {
  const validValues = {
    nic: '200114701234',
    firstName: 'Nimal',
    lastName: 'Perera',
    email: 'nimal@solgrid.lk',
    phoneNumber: '0771234567',
    password: 'sunny-grid-8',
  }

  it('accepts a complete create form', () => {
    expect(validateProsumerForm(validValues, true)).toEqual({})
  })

  it('skips the password when editing an existing prosumer', () => {
    expect(validateProsumerForm({ ...validValues, password: '' }, false)).toEqual({})
    expect(validateProsumerForm({ ...validValues, password: '' }, true)).toEqual({
      password: 'Password is required',
    })
  })

  it('reports every invalid field at once', () => {
    expect(validateProsumerForm({ ...validValues, nic: '12345', phoneNumber: '12345' }, true)).toEqual({
      nic: 'Enter 12 digits, or 9 digits followed by V or X',
      phoneNumber: 'Enter a 10-digit Sri Lankan phone number beginning with 0',
    })
  })
})
