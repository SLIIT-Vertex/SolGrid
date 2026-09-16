import { Link } from 'react-router-dom'

export function ForbiddenPage() {
  return (
    <div className="flex min-h-svh flex-col items-center justify-center gap-3 bg-ink-50 px-4 text-center">
      <p className="text-sm font-semibold text-brand-600">403</p>
      <h1 className="text-xl font-semibold text-ink-900">You don't have access to this page</h1>
      <p className="max-w-sm text-sm text-ink-500">
        This section is restricted to a different role. Contact a Backoffice administrator if you
        believe this is a mistake.
      </p>
      <Link to="/" className="mt-2 text-sm font-medium text-brand-600 hover:text-brand-700">
        Back to dashboard
      </Link>
    </div>
  )
}
