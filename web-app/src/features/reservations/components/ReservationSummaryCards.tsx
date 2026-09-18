import type { ReservationDashboardSummaryDto } from '../types'

const counts: {
  key: keyof ReservationDashboardSummaryDto
  label: string
  hint: string
}[] = [
  {
    key: 'pendingReservationsCount',
    label: 'Awaiting review',
    hint: 'Pending approval',
  },
  {
    key: 'approvedFutureReservationsCount',
    label: 'Upcoming',
    hint: 'Approved reservations',
  },
  {
    key: 'currentReservationsCount',
    label: 'Current',
    hint: 'Active reservations',
  },
  { key: 'bookingHistoryCount', label: 'History', hint: 'Past reservations' },
]

export function ReservationSummaryCards({
  summary,
  isLoading,
}: {
  summary?: ReservationDashboardSummaryDto
  isLoading: boolean
}) {
  return (
    <dl className="grid grid-cols-2 overflow-hidden rounded-xl border border-ink-200 bg-white lg:grid-cols-4">
      {counts.map((count, index) => (
        <div
          key={count.key}
          className="border-ink-100 px-5 py-5 sm:px-6 [&:nth-child(odd)]:border-r lg:border-r lg:last:border-r-0"
        >
          <dt className="flex items-center gap-2 text-sm text-ink-700">
            {index === 0 && (
              <span
                aria-hidden="true"
                className="size-1.5 rounded-full bg-amber-600"
              />
            )}
            {count.label}
          </dt>
          <dd className="mt-2 flex flex-wrap items-baseline gap-x-3 gap-y-1">
            <span className="text-2xl font-semibold tabular-nums text-ink-900">
              {isLoading ? (
                <span
                  className="inline-block h-7 w-10 rounded bg-ink-100 motion-safe:animate-pulse"
                  aria-label="Loading count"
                />
              ) : (
                (summary?.[count.key]?.toLocaleString() ?? '—')
              )}
            </span>
            <span className="text-xs text-ink-600">{count.hint}</span>
          </dd>
        </div>
      ))}
    </dl>
  )
}
