import { useLocation } from 'react-router-dom'
import { useAuth } from '@/auth/useAuth'

const titles: Record<string, string> = {
  '/analytics': 'Analytics',
  '/reservations': 'Reservations',
  '/users': 'Web Users',
  '/prosumers': 'Prosumers',
  '/microgrid': 'Microgrid Nodes',
  '/microgrid/new': 'New Microgrid Node',
}

export function useAppTitle(): string {
  const { pathname } = useLocation()
  const { session } = useAuth()
  if (pathname === '/') {
    return session?.role === 'Backoffice' ? 'Backoffice Dashboard' : 'Operator Dashboard'
  }
  if (titles[pathname]) return titles[pathname]
  if (pathname.startsWith('/users')) return 'Web Users'
  if (pathname.startsWith('/prosumers')) return 'Prosumers'
  if (pathname.startsWith('/analytics')) return 'Analytics'
  if (pathname.startsWith('/reservations')) return 'Reservations'
  if (pathname.startsWith('/microgrid/') && pathname.endsWith('/edit')) return 'Edit Microgrid Node'
  if (pathname.startsWith('/microgrid/')) return 'Microgrid Node'
  return 'SolGrid'
}
