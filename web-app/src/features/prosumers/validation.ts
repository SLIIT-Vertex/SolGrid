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

export type ProsumerFormErrors = Partial<Record<keyof ProsumerFormValues, string>>

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

export function validateSriLankanNic(value: string): string | undefined {
  const nic = value.trim().toUpperCase()
  if (/^\d{12}$/.test(nic) || /^\d{9}[VX]$/.test(nic)) return undefined
  return 'Enter 12 digits, or 9 digits followed by V or X'
}

export function validatePhoneNumber(value: string, required = false): string | undefined {
  const phone = value.trim()
  if (!phone) return required ? 'Phone number is required' : undefined
  if (!/^0\d{9}$/.test(phone)) return 'Enter a 10-digit Sri Lankan phone number beginning with 0'
  return undefined
}

export function validateProsumerForm(values: ProsumerFormValues, includePassword: boolean): ProsumerFormErrors {
  const errors: ProsumerFormErrors = {}
  const firstName = values.firstName.trim()
  const lastName = values.lastName.trim()
  const email = values.email.trim()

  const nicError = validateSriLankanNic(values.nic)
  if (nicError) errors.nic = nicError
  if (!firstName) errors.firstName = 'First name is required'
  else if (firstName.length > PROSUMER_NAME_MAX_LENGTH) {
    errors.firstName = `First name must be no more than ${PROSUMER_NAME_MAX_LENGTH} characters`
  }
  if (!lastName) errors.lastName = 'Last name is required'
  else if (lastName.length > PROSUMER_NAME_MAX_LENGTH) {
    errors.lastName = `Last name must be no more than ${PROSUMER_NAME_MAX_LENGTH} characters`
  }
  if (!email) errors.email = 'Email is required'
  else if (email.length > PROSUMER_EMAIL_MAX_LENGTH || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    errors.email = 'Enter a valid email address'
  }

  const phoneError = validatePhoneNumber(values.phoneNumber)
  if (phoneError) errors.phoneNumber = phoneError

  if (includePassword) {
    if (!values.password) errors.password = 'Password is required'
    else if (values.password.length < PROSUMER_PASSWORD_MIN_LENGTH) {
      errors.password = `Password must be at least ${PROSUMER_PASSWORD_MIN_LENGTH} characters`
    } else if (values.password.length > PROSUMER_PASSWORD_MAX_LENGTH) {
      errors.password = `Password must be no more than ${PROSUMER_PASSWORD_MAX_LENGTH} characters`
    }
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
