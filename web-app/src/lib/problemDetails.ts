import axios from 'axios'

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]> | string[]
}

function firstError(errors: Record<string, string[]> | string[]): string | undefined {
  if (Array.isArray(errors)) {
    return errors.find((message) => Boolean(message))
  }

  for (const messages of Object.values(errors)) {
    if (messages?.[0]) return messages[0]
  }

  return undefined
}

export function isNotFoundError(error: unknown): boolean {
  return axios.isAxiosError(error) && error.response?.status === 404
}

export function isConflictError(error: unknown): boolean {
  return axios.isAxiosError(error) && error.response?.status === 409
}

export function getErrorMessage(error: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (axios.isAxiosError<ProblemDetails>(error)) {
    const data = error.response?.data
    if (data?.errors) {
      const message = firstError(data.errors)
      if (message) return message
    }
    if (data?.detail) return data.detail
    if (data?.title) return data.title
    if (error.message) return error.message
  }
  return fallback
}
