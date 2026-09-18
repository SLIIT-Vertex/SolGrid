import { Link } from 'react-router-dom'
import { useAuth } from '@/auth/useAuth'
import adminIllustration from '@/assets/admin-dashboard.png'
import operatorIllustration from '@/assets/operator-dashboard.png'
import { useProsumerStatusCounts } from '@/features/prosumers/hooks/useProsumerStatusCounts'
import { useReservationDashboardSummary } from '@/features/reservations/hooks/useReservationDashboardSummary'

const backofficeLinks = [
  { to: '/users', label: 'Web Users', hint: 'Create and manage Backoffice and Grid Operator accounts.' },
  { to: '/prosumers', label: 'Prosumers', hint: 'Create, update, deactivate, and reactivate profiles.' },
  { to: '/prosumers?status=Pending', label: 'Pending activations', hint: 'Review mobile registrations waiting for activation.' },
  { to: '/microgrid', label: 'Microgrid Nodes', hint: 'Manage stations, schedules, and battery slots.' },
  { to: '/reservations', label: 'Reservations', hint: 'Create, update, and cancel energy bookings.' },
]

const operatorLinks = [
  { to: '/reservations', label: 'Reservations', hint: 'Monitor pending, approved, and current bookings.' },
  { to: '/microgrid', label: 'Station slots', hint: 'Review nodes and update battery slot availability.' },
]

function Stat({ label, value, loading }: { label: string; value?: number; loading: boolean }) {
  return (
    <div className="rounded-2xl border border-ink-100 bg-white p-5">
      <p className="text-sm text-ink-500">{label}</p>
      <p className="mt-1.5 text-2xl font-semibold text-ink-900">{loading ? '—' : (value ?? 0)}</p>
    </div>
  )
}

function Shortcuts({ items }: { items: { to: string; label: string; hint: string }[] }) {
  return (
    <div className="grid gap-3 sm:grid-cols-2">
      {items.map((item) => (
        <Link
          key={item.to}
          to={item.to}
          className="rounded-2xl border border-ink-100 bg-white p-5 transition-colors hover:border-ink-200"
        >
          <p className="font-medium text-ink-900">{item.label}</p>
          <p className="mt-1 text-sm text-ink-500">{item.hint}</p>
        </Link>
      ))}
    </div>
  )
}

function BackofficeDashboard() {
  const { session } = useAuth()
  const { data: counts, isLoading: countsLoading } = useProsumerStatusCounts()
  const { data: summary, isLoading: summaryLoading } = useReservationDashboardSummary()

  return (
    <div className="mx-auto max-w-5xl space-y-6">
      <div className="overflow-hidden rounded-2xl border border-ink-100 bg-white lg:grid lg:grid-cols-2">
        <div className="flex flex-col justify-center p-6 sm:p-8">
          <p className="text-sm font-medium text-brand-600">Welcome back</p>
          <h2 className="mt-1 text-2xl font-semibold text-ink-900">
            {session?.firstName} {session?.lastName}
          </h2>
          <p className="mt-2 max-w-md text-sm text-ink-500">
            Ready to manage users, prosumers, nodes, and reservations.
          </p>
        </div>
        <div className="flex items-center justify-center bg-brand-50 p-6">
          <img
            src={adminIllustration}
            alt="Backoffice administration overview"
            className="w-full max-w-md object-contain"
          />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4 lg:grid-cols-3">
        <Stat label="Pending activations" value={counts?.Pending} loading={countsLoading} />
        <Stat label="Pending reservations" value={summary?.pendingReservationsCount} loading={summaryLoading} />
        <Stat label="Approved (upcoming)" value={summary?.approvedFutureReservationsCount} loading={summaryLoading} />
      </div>

      <Shortcuts items={backofficeLinks} />
    </div>
  )
}

function OperatorDashboard() {
  const { session } = useAuth()
  const { data: summary, isLoading } = useReservationDashboardSummary()

  return (
    <div className="mx-auto max-w-5xl space-y-6">
      <div className="overflow-hidden rounded-2xl border border-ink-100 bg-white lg:grid lg:grid-cols-2">
        <div className="flex flex-col justify-center p-6 sm:p-8">
          <p className="text-sm font-medium text-brand-600">Welcome back</p>
          <h2 className="mt-1 text-2xl font-semibold text-ink-900">
            {session?.firstName} {session?.lastName}
          </h2>
          <p className="mt-2 max-w-md text-sm text-ink-500">
            Ready to review bookings and keep stations running.
          </p>
        </div>
        <div className="flex items-center justify-center bg-brand-50 p-6">
          <img
            src={operatorIllustration}
            alt="Grid operators monitoring the microgrid"
            className="w-full max-w-md object-contain"
          />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <Stat label="Pending reservations" value={summary?.pendingReservationsCount} loading={isLoading} />
        <Stat label="Approved (upcoming)" value={summary?.approvedFutureReservationsCount} loading={isLoading} />
      </div>

      <Shortcuts items={operatorLinks} />
    </div>
  )
}

export function DashboardPage() {
  const { session } = useAuth()
  if (!session) return null
  return session.role === 'Backoffice' ? <BackofficeDashboard /> : <OperatorDashboard />
}
