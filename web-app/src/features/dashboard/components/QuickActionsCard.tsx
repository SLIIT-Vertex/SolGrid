import { Link } from 'react-router-dom'
import { visibleQuickActions } from '@/features/dashboard/quickActions'
import type { UserRole } from '@/auth/types'

export function QuickActionsCard({ role }: { role: UserRole }) {
  const actions = visibleQuickActions(role)
  if (actions.length === 0) return null

  return (
    <section className="space-y-3">
      <h3 className="text-sm font-semibold text-ink-900">Quick actions</h3>
      <ul className="grid gap-3 sm:grid-cols-2">
        {actions.map((action) => (
          <li key={action.id}>
            <Link
              to={action.to}
              className="flex h-full items-start justify-between gap-3 rounded-2xl border border-ink-100 bg-white p-4 transition-colors hover:border-brand-200 hover:bg-brand-50/30 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600"
            >
              <span>
                <span className="block text-sm font-medium text-ink-900">{action.label}</span>
                <span className="mt-0.5 block text-xs text-ink-500">{action.description}</span>
              </span>
              <svg viewBox="0 0 20 20" fill="currentColor" className="mt-0.5 size-4 shrink-0 text-ink-300">
                <path
                  fillRule="evenodd"
                  d="M12.293 4.293a1 1 0 0 1 1.414 0l5 5a1 1 0 0 1 0 1.414l-5 5a1 1 0 0 1-1.414-1.414L15.586 11H3a1 1 0 1 1 0-2h12.586l-3.293-3.293a1 1 0 0 1 0-1.414Z"
                  clipRule="evenodd"
                />
              </svg>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  )
}
