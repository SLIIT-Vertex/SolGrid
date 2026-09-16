import { useLocation } from 'react-router-dom'

const titles: Record<string, string> = {
  '/': 'Dashboard',
  '/users': 'Web Users',
}

export function useAppTitle(): string {
  const { pathname } = useLocation()
  if (titles[pathname]) return titles[pathname]
  if (pathname.startsWith('/users')) return 'Web Users'
  return 'SolGrid'
}
