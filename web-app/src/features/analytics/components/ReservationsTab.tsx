import { BarChart, ChartCard, DonutChart, MeterBar } from '@/features/analytics/components/charts'
import { KpiStrip } from '@/features/analytics/components/KpiStrip'
import { chartColors, reservationColors } from '@/features/analytics/palette'
import type { AnalyticsSnapshot } from '@/features/analytics/types'

export function ReservationsTab({ snapshot }: { snapshot: AnalyticsSnapshot }) {
  const { reservations } = snapshot
  const live = reservations.pending + reservations.approved
  const closed = reservations.rejected + reservations.cancelled + reservations.completed
  const completionBase = reservations.completed + reservations.rejected + reservations.cancelled
  const completionRate = completionBase === 0 ? 0 : Math.round((reservations.completed / completionBase) * 100)

  return (
    <div className="space-y-6">
      <KpiStrip
        items={[
          { label: 'All bookings', value: reservations.total },
          { label: 'Live', value: live, hint: `${reservations.pending} pending` },
          { label: 'Closed', value: closed },
          {
            label: 'Past & still open',
            value: reservations.pastStillOpen,
            tone: reservations.pastStillOpen > 0 ? 'attention' : 'default',
          },
          {
            label: 'Beyond 7 days',
            value: reservations.beyondSevenDayWindow,
            tone: reservations.beyondSevenDayWindow > 0 ? 'danger' : 'default',
          },
        ]}
      />

      <div className="grid gap-4 lg:grid-cols-2">
        <ChartCard title="Lifecycle" description="Every booking by state.">
          <DonutChart
            centerValue={reservations.total}
            centerLabel="bookings"
            slices={[
              { label: 'Pending', value: reservations.pending, color: reservationColors.pending },
              { label: 'Approved', value: reservations.approved, color: reservationColors.approved },
              { label: 'Completed', value: reservations.completed, color: reservationColors.completed },
              { label: 'Rejected', value: reservations.rejected, color: reservationColors.rejected },
              { label: 'Cancelled', value: reservations.cancelled, color: reservationColors.cancelled },
            ]}
          />
          <div className="mt-5 border-t border-ink-100 pt-4">
            <MeterBar
              label="Completion rate"
              percent={completionRate}
              caption="Completed as a share of all closed bookings."
            />
          </div>
        </ChartCard>

        <ChartCard title="Trading windows" description="Live bookings measured against the 7-day and 12-hour rules.">
          <BarChart
            bars={[
              { label: 'Current (still ahead)', value: reservations.current, color: chartColors.brand },
              { label: 'Approved & upcoming', value: reservations.approvedFuture, color: chartColors.brandSoft },
              { label: 'Inside 7-day window', value: reservations.insideSevenDayWindow, color: chartColors.brand },
              { label: 'Changeable (12h+)', value: reservations.changeable, color: chartColors.ink },
              { label: 'Locked (<12h notice)', value: reservations.lockedByNotice, color: chartColors.amberSoft },
              { label: 'Beyond 7 days', value: reservations.beyondSevenDayWindow, color: chartColors.red },
            ]}
          />
        </ChartCard>

        <ChartCard title="QR dispatch" description="Transaction tokens from issue to completed transfer.">
          <BarChart
            bars={[
              { label: 'Awaiting a QR', value: reservations.approvedAwaitingQr, color: chartColors.amberSoft },
              { label: 'Token still valid', value: reservations.qrLive, color: chartColors.brand },
              { label: 'Verified, not done', value: reservations.qrVerified, color: chartColors.brandSoft },
              { label: 'Expired unused', value: reservations.qrExpired, color: chartColors.red },
            ]}
          />
        </ChartCard>

        <ChartCard title="History depth" description="How the closed record breaks down.">
          <BarChart
            bars={[
              { label: 'Completed', value: reservations.completed, color: reservationColors.completed },
              { label: 'Cancelled', value: reservations.cancelled, color: reservationColors.cancelled },
              { label: 'Rejected', value: reservations.rejected, color: reservationColors.rejected },
              { label: 'In history view', value: reservations.history, color: chartColors.ink },
            ]}
          />
        </ChartCard>
      </div>
    </div>
  )
}
