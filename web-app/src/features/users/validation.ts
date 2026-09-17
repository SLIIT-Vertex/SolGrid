export const USER_NAME_MAX_LENGTH = 100
export const USER_EMAIL_MAX_LENGTH = 254
export const USER_PASSWORD_MIN_LENGTH = 8
export const USER_PASSWORD_MAX_LENGTH = 128
export const USER_SEARCH_MAX_LENGTH = 100

interface UserFormTextValues {
  firstName: string
  lastName: string
  email: string
  password?: string
}

export function normalizeUserFormValues<T extends UserFormTextValues>(values: T): T {
  return {
    ...values,
    firstName: values.firstName.trim(),
    lastName: values.lastName.trim(),
    email: values.email.trim().toLowerCase(),
  }
}

export function validateName(value: string, label: string): true | string {
  const normalizedValue = value.trim()
  if (!normalizedValue) return `${label} is required`
  if (normalizedValue.length > USER_NAME_MAX_LENGTH) {
    return `${label} must be no more than ${USER_NAME_MAX_LENGTH} characters`
  }
  return true
}

export function validateEmail(value: string): true | string {
  const normalizedValue = value.trim()
  if (!normalizedValue) return 'Email is required'
  if (normalizedValue.length > USER_EMAIL_MAX_LENGTH) {
    return `Email must be no more than ${USER_EMAIL_MAX_LENGTH} characters`
  }
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(normalizedValue)) {
    return 'Enter a valid email address'
  }
  return true
}

export function validatePassword(value: string | undefined): true | string {
  if (!value) return 'Password is required'
  if (value.length < USER_PASSWORD_MIN_LENGTH) {
    return `Password must be at least ${USER_PASSWORD_MIN_LENGTH} characters`
  }
  if (value.length > USER_PASSWORD_MAX_LENGTH) {
    return `Password must be no more than ${USER_PASSWORD_MAX_LENGTH} characters`
  }
  return true
}
