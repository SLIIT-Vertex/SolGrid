import { useLocation } from 'react-router-dom'

const titles: Record<string, string> = {
  '/': 'Dashboard',
  '/users': 'Web Users',
  '/prosumers': 'Prosumer Accounts',
  '/reservations': 'Reservations',
  '/microgrid': 'Microgrid Nodes',
  '/microgrid/new': 'New Microgrid Node',
}

export function useAppTitle(): string {
  const { pathname } = useLocation()
  if (titles[pathname]) return titles[pathname]
  if (pathname.startsWith('/users')) return 'Web Users'
  if (pathname.startsWith('/prosumers')) return 'Prosumer Accounts'
  if (pathname.startsWith('/reservations')) return 'Reservations'
  if (pathname.startsWith('/microgrid/') && pathname.endsWith('/edit')) return 'Edit Microgrid Node'
  if (pathname.startsWith('/microgrid/')) return 'Microgrid Node'
  return 'SolGrid'
}
