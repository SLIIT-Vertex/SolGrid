import { Link } from 'react-router-dom'
import { useAuth } from '@/auth/useAuth'

export function DashboardPage() {
  const { session } = useAuth()

  return (
    <div className="mx-auto max-w-3xl">
      <div className="rounded-2xl border border-ink-100 bg-white p-8">
        <p className="text-sm font-medium text-brand-600">Welcome back</p>
        <h2 className="mt-1 text-2xl font-semibold text-ink-900">
          {session?.firstName} {session?.lastName}
        </h2>
        <p className="mt-2 text-sm text-ink-500">
          You are signed in as <span className="font-medium text-ink-700">{session?.role}</span>.
          Use Web Users to manage Backoffice and Grid Operator accounts.
        </p>
        <Link
          to="/users"
          className="mt-6 inline-flex items-center gap-1.5 text-sm font-medium text-brand-600 hover:text-brand-700"
        >
          Go to Web Users
          <svg viewBox="0 0 20 20" fill="currentColor" className="size-4">
            <path
              fillRule="evenodd"
              d="M12.293 4.293a1 1 0 0 1 1.414 0l5 5a1 1 0 0 1 0 1.414l-5 5a1 1 0 0 1-1.414-1.414L15.586 11H3a1 1 0 1 1 0-2h12.586l-3.293-3.293a1 1 0 0 1 0-1.414Z"
              clipRule="evenodd"
            />
          </svg>
        </Link>
      </div>
    </div>
  )
}
