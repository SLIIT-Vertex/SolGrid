import { useAuth } from '@/auth/useAuth'
import adminIllustration from '@/assets/admin-dashboard.png'
import operatorIllustration from '@/assets/operator-dashboard.png'
import { BackofficeMetrics } from '@/features/dashboard/components/BackofficeMetrics'
import { MicrogridMetrics } from '@/features/dashboard/components/MicrogridMetrics'
import { QuickActionsCard } from '@/features/dashboard/components/QuickActionsCard'
import { ReservationMetrics } from '@/features/dashboard/components/ReservationMetrics'
import { roleIntro } from '@/features/dashboard/quickActions'

function WelcomeHero({ illustration, alt }: { illustration: string; alt: string }) {
  const { session } = useAuth()
  if (!session) return null

  return (
    <div className="overflow-hidden rounded-2xl border border-ink-100 bg-white lg:grid lg:grid-cols-2">
      <div className="flex flex-col justify-center p-6 sm:p-8">
        <p className="text-sm font-medium text-brand-600">Welcome back</p>
        <h2 className="mt-1 text-2xl font-semibold text-ink-900">
          {session.firstName} {session.lastName}
        </h2>
        <p className="mt-2 max-w-md text-sm text-ink-500">{roleIntro[session.role]}</p>
      </div>
      <div className="flex items-center justify-center bg-brand-50 p-6">
        <img src={illustration} alt={alt} className="w-full max-w-md object-contain" />
      </div>
    </div>
  )
}

function BackofficeDashboard() {
  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <WelcomeHero illustration={adminIllustration} alt="Backoffice administration overview" />
      <ReservationMetrics />
      <MicrogridMetrics />
      <BackofficeMetrics />
      <QuickActionsCard role="Backoffice" />
    </div>
  )
}

function OperatorDashboard() {
  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <WelcomeHero illustration={operatorIllustration} alt="Grid operators monitoring the microgrid" />
      <ReservationMetrics />
      <MicrogridMetrics />
      <QuickActionsCard role="GridOperator" />
    </div>
  )
}

export function DashboardPage() {
  const { session } = useAuth()
  if (!session) return null
  return session.role === 'Backoffice' ? <BackofficeDashboard /> : <OperatorDashboard />
}
