import { NavLink } from 'react-router-dom'
import { Logo } from '@/components/layout/Logo'
import { cn } from '@/lib/cn'

interface NavItem {
  to: string
  label: string
  icon: (props: { className?: string }) => React.JSX.Element
}

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

const navItems: NavItem[] = [
  { to: '/', label: 'Dashboard', icon: DashboardIcon },
  { to: '/microgrid', label: 'Microgrid Nodes', icon: MicrogridIcon },
  { to: '/users', label: 'Web Users', icon: UsersIcon },
]

export function Sidebar() {
  return (
    <aside className="hidden w-64 shrink-0 flex-col border-r border-ink-100 bg-white lg:flex">
      <div className="flex h-16 items-center gap-2.5 px-6">
        <Logo />
        <div>
          <p className="text-sm font-semibold text-ink-900">SolGrid</p>
          <p className="text-xs text-ink-400">Backoffice Console</p>
        </div>
      </div>
      <nav className="flex flex-1 flex-col gap-1 px-3 py-4">
        {navItems.map((item) => (
          <NavLink
            key={item.to}
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
            <item.icon className="size-4.5 shrink-0" />
            {item.label}
          </NavLink>
        ))}
      </nav>
    </aside>
  )
}
