export const PROSUMER_NIC_MAX_LENGTH = 12
export const PROSUMER_NAME_MAX_LENGTH = 100
export const PROSUMER_EMAIL_MAX_LENGTH = 254
export const PROSUMER_PHONE_MAX_LENGTH = 10
export const PROSUMER_PASSWORD_MIN_LENGTH = 8
export const PROSUMER_PASSWORD_MAX_LENGTH = 128

export interface ProsumerFormValues {
  nic: string
  firstName: string
  lastName: string
  email: string
  phoneNumber: string
  password: string
}

export type ProsumerFormField = keyof ProsumerFormValues

export type ProsumerFormErrors = Partial<Record<ProsumerFormField, string>>

export function sanitizeNicInput(value: string): string {
  const allowed = value.toUpperCase().replace(/[^0-9VX]/g, '')
  const letterIndex = allowed.search(/[VX]/)
  if (letterIndex < 0) return allowed.slice(0, PROSUMER_NIC_MAX_LENGTH)

  const digits = allowed.slice(0, letterIndex).replace(/\D/g, '').slice(0, 9)
  return digits.length === 9 ? `${digits}${allowed[letterIndex]}` : digits
}

export function sanitizePhoneInput(value: string): string {
  return value.replace(/\D/g, '').slice(0, PROSUMER_PHONE_MAX_LENGTH)
}

export function sanitizeNameInput(value: string): string {
  return value
    .replace(/[^\p{L}\p{M}\s'.-]/gu, '')
    .replace(/^\s+/, '')
    .replace(/\s{2,}/g, ' ')
    .slice(0, PROSUMER_NAME_MAX_LENGTH)
}

export function sanitizeEmailInput(value: string): string {
  return value.replace(/\s+/g, '').slice(0, PROSUMER_EMAIL_MAX_LENGTH)
}

export function validateSriLankanNic(value: string): string | undefined {
  const nic = value.trim().toUpperCase()
  if (!nic) return 'NIC is required'
  if (/^\d{12}$/.test(nic) || /^\d{9}[VX]$/.test(nic)) return undefined
  return 'Enter 12 digits, or 9 digits followed by V or X'
}

export function validateProsumerName(value: string, label: string): string | undefined {
  const name = value.trim()
  if (!name) return `${label} is required`
  if (name.length > PROSUMER_NAME_MAX_LENGTH) {
    return `${label} must be no more than ${PROSUMER_NAME_MAX_LENGTH} characters`
  }
  if (!/^\p{L}[\p{L}\p{M}\s'.-]*$/u.test(name)) {
    return `${label} may only contain letters, spaces, apostrophes, and hyphens`
  }
  return undefined
}

export function validateProsumerEmail(value: string): string | undefined {
  const email = value.trim()
  if (!email) return 'Email is required'
  if (email.length > PROSUMER_EMAIL_MAX_LENGTH) {
    return `Email must be no more than ${PROSUMER_EMAIL_MAX_LENGTH} characters`
  }
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(email)) return 'Enter a valid email address'
  return undefined
}

export function validatePhoneNumber(value: string, required = false): string | undefined {
  const phone = value.trim()
  if (!phone) return required ? 'Phone number is required' : undefined
  if (!/^0\d{9}$/.test(phone)) return 'Enter a 10-digit Sri Lankan phone number beginning with 0'
  return undefined
}

export function validateProsumerPassword(value: string): string | undefined {
  if (!value) return 'Password is required'
  if (value.length < PROSUMER_PASSWORD_MIN_LENGTH) {
    return `Password must be at least ${PROSUMER_PASSWORD_MIN_LENGTH} characters`
  }
  if (value.length > PROSUMER_PASSWORD_MAX_LENGTH) {
    return `Password must be no more than ${PROSUMER_PASSWORD_MAX_LENGTH} characters`
  }
  return undefined
}

/** Validate a single field so the dialog can report problems as the user leaves each input. */
export function validateProsumerField(
  field: ProsumerFormField,
  values: ProsumerFormValues,
  includePassword: boolean,
): string | undefined {
  switch (field) {
    case 'nic':
      return validateSriLankanNic(values.nic)
    case 'firstName':
      return validateProsumerName(values.firstName, 'First name')
    case 'lastName':
      return validateProsumerName(values.lastName, 'Last name')
    case 'email':
      return validateProsumerEmail(values.email)
    case 'phoneNumber':
      return validatePhoneNumber(values.phoneNumber)
    case 'password':
      return includePassword ? validateProsumerPassword(values.password) : undefined
  }
}

const PROSUMER_FORM_FIELDS: ProsumerFormField[] = [
  'nic',
  'firstName',
  'lastName',
  'email',
  'phoneNumber',
  'password',
]

export function validateProsumerForm(values: ProsumerFormValues, includePassword: boolean): ProsumerFormErrors {
  const errors: ProsumerFormErrors = {}
  for (const field of PROSUMER_FORM_FIELDS) {
    const message = validateProsumerField(field, values, includePassword)
    if (message) errors[field] = message
  }

  return errors
}

export function normalizeProsumerForm(values: ProsumerFormValues): ProsumerFormValues {
  return {
    ...values,
    nic: values.nic.trim().toUpperCase(),
    firstName: values.firstName.trim(),
    lastName: values.lastName.trim(),
    email: values.email.trim().toLowerCase(),
    phoneNumber: values.phoneNumber.trim(),
  }
}
