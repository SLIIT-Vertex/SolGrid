import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/auth/useAuth'

function initials(firstName: string, lastName: string): string {
  return `${firstName.charAt(0)}${lastName.charAt(0)}`.toUpperCase()
}

export function Topbar({ title }: { title: string }) {
  const { session, logout } = useAuth()
  const navigate = useNavigate()
  const [menuOpen, setMenuOpen] = useState(false)

  if (!session) return null

  return (
    <header className="flex h-16 shrink-0 items-center justify-between border-b border-ink-100 bg-white px-6">
      <h1 className="text-lg font-semibold text-ink-900">{title}</h1>
      <div className="relative">
        <button
          type="button"
          onClick={() => setMenuOpen((open) => !open)}
          className="flex items-center gap-3 rounded-full py-1 pl-1 pr-3 hover:bg-ink-50"
        >
          <span className="flex size-8 items-center justify-center rounded-full bg-brand-600 text-xs font-semibold text-white">
            {initials(session.firstName, session.lastName)}
          </span>
          <span className="hidden text-left sm:block">
            <span className="block text-sm font-medium text-ink-900">
              {session.firstName} {session.lastName}
            </span>
            <span className="block text-xs text-ink-400">{session.role}</span>
          </span>
        </button>
        {menuOpen ? (
          <>
            <div className="fixed inset-0 z-10" onClick={() => setMenuOpen(false)} />
            <div className="absolute right-0 z-20 mt-2 w-48 rounded-lg border border-ink-100 bg-white py-1 shadow-lg">
              <div className="border-b border-ink-100 px-3 py-2">
                <p className="truncate text-sm text-ink-700">{session.email}</p>
              </div>
              <button
                type="button"
                onClick={() => {
                  logout()
                  navigate('/login')
                }}
                className="w-full px-3 py-2 text-left text-sm text-red-600 hover:bg-red-50"
              >
                Sign out
              </button>
            </div>
          </>
        ) : null}
      </div>
    </header>
  )
}
