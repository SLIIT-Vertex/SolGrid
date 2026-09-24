import { formatGenerationKw, formatStorageKwh } from '@/features/microgrid/format'
import { BarChart, ChartCard, DonutChart, MeterBar } from '@/features/analytics/components/charts'
import { KpiStrip } from '@/features/analytics/components/KpiStrip'
import { chartColors, reservationColors, slotColors } from '@/features/analytics/palette'
import type { AnalyticsSnapshot } from '@/features/analytics/types'

export function OverviewTab({ snapshot }: { snapshot: AnalyticsSnapshot }) {
  const { network, reservations, routines } = snapshot
  const bookableSlots = network.availableSlots + network.reservedSlots + network.occupiedSlots
  const heldSlots = network.reservedSlots + network.occupiedSlots
  const utilization = bookableSlots === 0 ? 0 : Math.round((heldSlots / bookableSlots) * 100)
  const attention = routines.filter((routine) => routine.outcome === 'Attention').length
  const breaches = routines.filter((routine) => routine.outcome === 'Breach').length

  return (
    <div className="space-y-6">
      <KpiStrip
        items={[
          { label: 'Active nodes', value: network.activeNodes, hint: `of ${network.nodeCount} registered` },
          { label: 'Generation', value: formatGenerationKw(network.totalCapacityKw) },
          {
            label: 'Battery stored',
            value: formatStorageKwh(network.totalBatteryKwh),
            hint: `${formatStorageKwh(network.committedBatteryKwh)} held now`,
          },
          { label: 'Live reservations', value: reservations.pending + reservations.approved, hint: `${reservations.pending} pending` },
          {
            label: 'Rule flags',
            value: breaches + attention,
            hint: `${breaches} breaches`,
            tone: breaches > 0 ? 'danger' : attention > 0 ? 'attention' : 'default',
          },
        ]}
      />

      <div className="grid gap-4 lg:grid-cols-2">
        <ChartCard title="Battery slots" description="Every slot across the network by state.">
          <DonutChart
            centerValue={network.totalSlots}
            centerLabel="slots"
            slices={[
              { label: 'Available', value: network.availableSlots, color: slotColors.available },
              { label: 'Reserved', value: network.reservedSlots, color: slotColors.reserved },
              { label: 'Occupied', value: network.occupiedSlots, color: slotColors.occupied },
              { label: 'Out of service', value: network.outOfServiceSlots, color: slotColors.outOfService },
            ]}
          />
          <div className="mt-5 border-t border-ink-100 pt-4">
            <MeterBar
              label="Slot utilization"
              percent={utilization}
              caption={`${heldSlots} of ${bookableSlots} bookable slots are reserved or occupied.`}
            />
          </div>
        </ChartCard>

        <ChartCard title="Reservations" description="Every booking by lifecycle state.">
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
        </ChartCard>

        <ChartCard title="Booking pipeline" description="Where live bookings sit against the trading rules.">
          <BarChart
            bars={[
              { label: 'Current (still ahead)', value: reservations.current, color: chartColors.brand },
              { label: 'Approved & upcoming', value: reservations.approvedFuture, color: chartColors.brandSoft },
              { label: 'Inside 7-day window', value: reservations.insideSevenDayWindow, color: chartColors.brand },
              { label: 'Changeable (12h+)', value: reservations.changeable, color: chartColors.ink },
              { label: 'Locked (<12h)', value: reservations.lockedByNotice, color: chartColors.amberSoft },
              { label: 'Beyond 7 days', value: reservations.beyondSevenDayWindow, color: chartColors.red },
            ]}
          />
        </ChartCard>

        <ChartCard title="QR dispatch" description="Transaction tokens from issue to completion.">
          <BarChart
            bars={[
              { label: 'Awaiting a QR', value: reservations.approvedAwaitingQr, color: chartColors.amberSoft },
              { label: 'Token still valid', value: reservations.qrLive, color: chartColors.brand },
              { label: 'Verified, not done', value: reservations.qrVerified, color: chartColors.brandSoft },
              { label: 'Expired unused', value: reservations.qrExpired, color: chartColors.red },
            ]}
          />
        </ChartCard>
      </div>
    </div>
  )
}
