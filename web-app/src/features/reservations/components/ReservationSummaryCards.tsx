import type { ReservationDashboardSummaryDto } from '@/features/reservations/types'

interface ReservationSummaryCardsProps {
  summary: ReservationDashboardSummaryDto | undefined
  isLoading: boolean
}

const cards: { key: keyof ReservationDashboardSummaryDto; label: string }[] = [
  { key: 'pendingReservationsCount', label: 'Pending review' },
  { key: 'approvedFutureReservationsCount', label: 'Approved (upcoming)' },
  { key: 'currentReservationsCount', label: 'Current' },
  { key: 'bookingHistoryCount', label: 'History' },
]

export function ReservationSummaryCards({ summary, isLoading }: ReservationSummaryCardsProps) {
  return (
    <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
      {cards.map((card) => (
        <div key={card.key} className="rounded-2xl border border-ink-100 bg-white p-5">
          <p className="text-sm text-ink-500">{card.label}</p>
          <p className="mt-1.5 text-2xl font-semibold text-ink-900">
            {isLoading ? '—' : (summary?.[card.key] ?? 0)}
          </p>
        </div>
      ))}
    </div>
  )
}
