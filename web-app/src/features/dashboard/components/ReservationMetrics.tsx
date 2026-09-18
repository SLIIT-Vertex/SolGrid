import { DashboardSection } from '@/features/dashboard/components/DashboardSection'
import { StatCard } from '@/features/dashboard/components/StatCard'
import { useReservationDashboardSummary } from '@/features/reservations/hooks/useReservationDashboardSummary'

/** Both roles may review reservations, so this section is shown to everyone. */
export function ReservationMetrics() {
  const { data, isLoading, isError } = useReservationDashboardSummary()

  return (
    <DashboardSection title="Reservations" description="Booking requests across every microgrid node.">
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <StatCard
          label="Pending review"
          value={data?.pendingReservationsCount}
          hint="Waiting on approval"
          to="/reservations"
          isLoading={isLoading}
          isError={isError}
          needsAttention
        />
        <StatCard
          label="Current"
          value={data?.currentReservationsCount}
          hint="In progress now"
          to="/reservations"
          isLoading={isLoading}
          isError={isError}
        />
        <StatCard
          label="Approved (upcoming)"
          value={data?.approvedFutureReservationsCount}
          hint="Scheduled ahead"
          to="/reservations"
          isLoading={isLoading}
          isError={isError}
        />
        <StatCard
          label="History"
          value={data?.bookingHistoryCount}
          hint="Completed or closed"
          to="/reservations"
          isLoading={isLoading}
          isError={isError}
        />
      </div>
    </DashboardSection>
  )
}
