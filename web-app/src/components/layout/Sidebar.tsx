import { NavLink } from 'react-router-dom'
import { roleLabel } from '@/auth/types'
import { useAuth } from '@/auth/useAuth'
import { Logo } from '@/components/layout/Logo'
import { visibleNavItems } from '@/components/layout/navItems'
import type { NavItemId } from '@/components/layout/navItems'
import { cn } from '@/lib/cn'

function DashboardIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 20 20" fill="currentColor" className={className}>
      <path d="M3 4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v4a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V4Zm0 8a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v4a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1v-4Zm8-8a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v4a1 1 0 0 1-1 1h-4a1 1 0 0 1-1-1V4Zm0 8a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v4a1 1 0 0 1-1 1h-4a1 1 0 0 1-1-1v-4Z" />
    </svg>
  )
}

function UsersIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 20 20" fill="currentColor" className={className}>
      <path d="M10 9a4 4 0 1 0 0-8 4 4 0 0 0 0 8ZM3.5 18a6.5 6.5 0 0 1 13 0 .75.75 0 0 1-.75.75h-11.5A.75.75 0 0 1 3.5 18Z" />
    </svg>
  )
}

function MicrogridIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 20 20" fill="currentColor" className={className}>
      <path d="M10 2.25a.75.75 0 0 1 .67.415l1.86 3.77 4.16.605a.75.75 0 0 1 .416 1.279l-3.01 2.934.71 4.145a.75.75 0 0 1-1.088.79L10 14.22l-3.718 1.968a.75.75 0 0 1-1.088-.79l.71-4.145-3.01-2.934a.75.75 0 0 1 .416-1.28l4.16-.604 1.86-3.77A.75.75 0 0 1 10 2.25Z" />
    </svg>
  )
}

function ProsumersIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 20 20" fill="currentColor" className={className}>
      <path d="M10 2a1 1 0 0 1 1 1v.06a6.5 6.5 0 0 1 5.5 6.44V13a1 1 0 0 1-1 1H4.5a1 1 0 0 1-1-1V9.5A6.5 6.5 0 0 1 9 3.06V3a1 1 0 0 1 1-1Zm-3 13a3 3 0 0 0 6 0H7Z" />
    </svg>
  )
}

function ReservationsIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 20 20" fill="currentColor" className={className}>
      <path d="M5.75 2a.75.75 0 0 1 .75.75V4h7V2.75a.75.75 0 0 1 1.5 0V4h.25A2.75 2.75 0 0 1 18 6.75v8.5A2.75 2.75 0 0 1 15.25 18H4.75A2.75 2.75 0 0 1 2 15.25v-8.5A2.75 2.75 0 0 1 4.75 4H5V2.75A.75.75 0 0 1 5.75 2ZM3.5 8.5v6.75c0 .69.56 1.25 1.25 1.25h10.5c.69 0 1.25-.56 1.25-1.25V8.5h-13Z" />
    </svg>
  )
}

const navIcons: Record<NavItemId, (props: { className?: string }) => React.JSX.Element> = {
  dashboard: DashboardIcon,
  reservations: ReservationsIcon,
  microgrid: MicrogridIcon,
  users: UsersIcon,
  prosumers: ProsumersIcon,
}

export function Sidebar() {
  const { session } = useAuth()
  if (!session) return null

  const items = visibleNavItems(session.role)

  return (
    <aside className="hidden w-64 shrink-0 flex-col border-r border-ink-100 bg-white lg:flex">
      <div className="flex h-16 items-center gap-2.5 px-6">
        <Logo />
        <div>
          <p className="text-sm font-semibold text-ink-900">SolGrid</p>
          <p className="text-xs text-ink-400">{roleLabel(session.role)} Console</p>
        </div>
      </div>
      <nav className="flex flex-1 flex-col gap-1 px-3 py-4">
        {items.map((item) => {
          const Icon = navIcons[item.id]
          return (
            <NavLink
              key={item.id}
              to={item.to}
              end={item.to === '/'}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-brand-50 text-brand-700'
                    : 'text-ink-600 hover:bg-ink-100 hover:text-ink-900',
                )
              }
            >
              <Icon className="size-4.5 shrink-0" />
              {item.label}
            </NavLink>
          )
        })}
      </nav>
    </aside>
  )
}
