import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '@/auth/useAuth'
import type { UserRole } from '@/auth/types'

interface ProtectedRouteProps {
  allowedRoles?: UserRole[]
}

export function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
  const { session, isAuthenticated } = useAuth()
  const location = useLocation()

  if (!isAuthenticated || !session) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  if (allowedRoles && !allowedRoles.includes(session.role)) {
    return <Navigate to="/403" replace />
  }

  return <Outlet />
}
