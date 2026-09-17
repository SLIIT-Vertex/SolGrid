export function digitsOnly(value: string, maximumLength?: number): string {
  const digits = value.replace(/\D/g, '')
  return maximumLength === undefined ? digits : digits.slice(0, maximumLength)
}

export function unsignedDecimal(value: string): string {
  const allowed = value.replace(/[^\d.]/g, '')
  const decimalIndex = allowed.indexOf('.')
  if (decimalIndex < 0) return allowed
  return `${allowed.slice(0, decimalIndex + 1)}${allowed.slice(decimalIndex + 1).replace(/\./g, '')}`
}
