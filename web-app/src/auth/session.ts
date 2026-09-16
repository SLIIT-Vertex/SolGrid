import type { AuthSession } from '@/auth/types'

const STORAGE_KEY = 'solgrid.session'

export function getStoredSession(): AuthSession | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    return JSON.parse(raw) as AuthSession
  } catch {
    return null
  }
}

export function getStoredToken(): string | null {
  return getStoredSession()?.token ?? null
}

export function storeSession(session: AuthSession): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
}

export function clearSession(): void {
  localStorage.removeItem(STORAGE_KEY)
}
