import axios from 'axios'

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export function getErrorMessage(error: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (axios.isAxiosError<ProblemDetails>(error)) {
    const data = error.response?.data
    if (data?.errors) {
      const firstField = Object.values(data.errors)[0]
      if (firstField?.[0]) return firstField[0]
    }
    if (data?.detail) return data.detail
    if (data?.title) return data.title
    if (error.message) return error.message
  }
  return fallback
}
