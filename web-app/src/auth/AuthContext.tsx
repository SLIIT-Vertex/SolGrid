import { createContext, useCallback, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import * as authApi from '@/auth/api'
import { clearSession, getStoredSession, storeSession } from '@/auth/session'
import type { AuthSession, LoginRequest } from '@/auth/types'

interface AuthContextValue {
  session: AuthSession | null
  isAuthenticated: boolean
  login: (request: LoginRequest) => Promise<AuthSession>
  logout: () => void
}

// eslint-disable-next-line react-refresh/only-export-components
export const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(() => getStoredSession())

  const login = useCallback(async (request: LoginRequest) => {
    const newSession = await authApi.login(request)
    storeSession(newSession)
    setSession(newSession)
    return newSession
  }, [])

  const logout = useCallback(() => {
    clearSession()
    setSession(null)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      isAuthenticated: session !== null,
      login,
      logout,
    }),
    [session, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
