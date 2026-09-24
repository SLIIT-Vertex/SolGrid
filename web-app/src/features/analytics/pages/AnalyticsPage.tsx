import { ErrorState, LoadingState } from '@/components/common/QueryStates'
import { formatDateTime, formatGenerationKw, formatStorageKwh } from '@/features/microgrid/format'
import { NodeAnalyticsTable } from '@/features/analytics/components/NodeAnalyticsTable'
import { RoutineLedger } from '@/features/analytics/components/RoutineLedger'
import { useAnalytics } from '@/features/analytics/hooks/useAnalytics'
import type { AccountAnalytics, ReservationAnalytics } from '@/features/analytics/types'

const reservationRows: { label: string; key: keyof ReservationAnalytics }[] = [
  { label: 'All reservations', key: 'total' },
  { label: 'Pending', key: 'pending' },
  { label: 'Approved', key: 'approved' },
  { label: 'Rejected', key: 'rejected' },
  { label: 'Cancelled', key: 'cancelled' },
  { label: 'Completed', key: 'completed' },
  { label: 'Current (still ahead)', key: 'current' },
  { label: 'Approved and upcoming', key: 'approvedFuture' },
  { label: 'History', key: 'history' },
  { label: 'Inside the 7-day window', key: 'insideSevenDayWindow' },
  { label: 'Beyond 7 days', key: 'beyondSevenDayWindow' },
  { label: 'Changeable (12 hours or more)', key: 'changeable' },
  { label: 'Locked inside 12 hours', key: 'lockedByNotice' },
  { label: 'Past and still open', key: 'pastStillOpen' },
  { label: 'Approved, waiting for a QR', key: 'approvedAwaitingQr' },
  { label: 'QR tokens still valid', key: 'qrLive' },
  { label: 'QR tokens expired', key: 'qrExpired' },
  { label: 'QR verified, not completed', key: 'qrVerified' },
]

export function AnalyticsPage() {
  const { data, isLoading, isError, refetch } = useAnalytics()

  if (isLoading) return <LoadingState label="Loading analytics…" />
  if (isError || !data) {
    return <ErrorState message="We couldn't load analytics." onRetry={() => refetch()} />
  }

  const { network, reservations } = data

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <p className="max-w-2xl text-sm text-ink-500">
        Figures calculated by the service at {formatDateTime(data.generatedAtUtc)}. Open a node row for its slots and reservation mix.
      </p>

      <section className="grid gap-4 lg:grid-cols-2">
        <figure className="rounded-2xl border border-ink-100 bg-white p-5">
          <figcaption className="text-sm font-semibold text-ink-900">Network</figcaption>
          <dl className="mt-3 grid grid-cols-2 gap-x-6 gap-y-2 text-sm text-ink-700">
            <dt>Nodes</dt><dd className="text-right tabular-nums">{network.activeNodes} active / {network.nodeCount}</dd>
            <dt>Inactive</dt><dd className="text-right tabular-nums">{network.inactiveNodes}</dd>
            <dt>Open slots</dt><dd className="text-right tabular-nums">{network.nodesWithOpenSlots} nodes</dd>
            <dt>Generation</dt><dd className="text-right tabular-nums">{formatGenerationKw(network.totalCapacityKw)}</dd>
            <dt>Battery installed</dt><dd className="text-right tabular-nums">{formatStorageKwh(network.totalBatteryKwh)}</dd>
            <dt>Battery held</dt><dd className="text-right tabular-nums">{formatStorageKwh(network.committedBatteryKwh)}</dd>
            <dt>Slots</dt>
            <dd className="text-right tabular-nums">
              {network.availableSlots} free, {network.reservedSlots} reserved, {network.occupiedSlots} occupied, {network.outOfServiceSlots} out
            </dd>
          </dl>
        </figure>
        <figure className="rounded-2xl border border-ink-100 bg-white p-5">
          <figcaption className="text-sm font-semibold text-ink-900">Reservations</figcaption>
          <dl className="mt-3 max-h-72 space-y-1 overflow-y-auto pr-1 text-sm text-ink-700">
            {reservationRows.map((row) => (
              <div key={row.key} className="flex justify-between gap-4">
                <dt>{row.label}</dt>
                <dd className="tabular-nums">{reservations[row.key]}</dd>
              </div>
            ))}
          </dl>
        </figure>
      </section>

      <RoutineLedger routines={data.routines} />
      <NodeAnalyticsTable nodes={data.nodes} />
      {data.accounts ? <AccountSection accounts={data.accounts} /> : null}
    </div>
  )
}

function AccountSection({ accounts }: { accounts: AccountAnalytics }) {
  const rows = [
    ['Web users', accounts.webUsers],
    ['Active web users', accounts.activeWebUsers],
    ['Backoffice', accounts.backofficeUsers],
    ['Grid Operators', accounts.gridOperatorUsers],
    ['Prosumers', accounts.prosumers],
    ['Awaiting activation', accounts.pendingProsumers],
    ['Active prosumers', accounts.activeProsumers],
    ['Deactivation requested', accounts.deactivationRequested],
    ['Deactivated', accounts.deactivatedProsumers],
  ] as const

  return (
    <section className="space-y-3">
      <div>
        <h2 className="text-sm font-semibold text-ink-900">Accounts</h2>
        <p className="text-sm text-ink-500">Staff and prosumer registers. Visible to Backoffice only.</p>
      </div>
      <dl className="grid gap-px overflow-hidden rounded-2xl border border-ink-100 bg-ink-100 sm:grid-cols-3">
        {rows.map(([label, value]) => (
          <div key={label} className="bg-white px-4 py-3">
            <dt className="text-xs text-ink-500">{label}</dt>
            <dd className="mt-1 text-lg font-semibold tabular-nums text-ink-900">{value}</dd>
          </div>
        ))}
      </dl>
    </section>
  )
}
