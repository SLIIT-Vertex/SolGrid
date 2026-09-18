import { useAuth } from '@/auth/useAuth'
import { BackofficeMetrics } from '@/features/dashboard/components/BackofficeMetrics'
import { MicrogridMetrics } from '@/features/dashboard/components/MicrogridMetrics'
import { QuickActionsCard } from '@/features/dashboard/components/QuickActionsCard'
import { ReservationMetrics } from '@/features/dashboard/components/ReservationMetrics'
import { roleIntro } from '@/features/dashboard/quickActions'

export function DashboardPage() {
  const { session } = useAuth()
  if (!session) return null

  const isBackoffice = session.role === 'Backoffice'

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <div className="rounded-2xl border border-ink-100 bg-white p-6">
        <p className="text-sm font-medium text-brand-600">Welcome back</p>
        <h2 className="mt-1 text-2xl font-semibold text-ink-900">
          {session.firstName} {session.lastName}
        </h2>
        <p className="mt-2 max-w-2xl text-sm text-ink-500">
          Signed in as <span className="font-medium text-ink-700">{session.role}</span>.{' '}
          {roleIntro[session.role]}
        </p>
      </div>

      <ReservationMetrics />
      <MicrogridMetrics />
      {isBackoffice ? <BackofficeMetrics /> : null}
      <QuickActionsCard role={session.role} />
    </div>
  )
}
