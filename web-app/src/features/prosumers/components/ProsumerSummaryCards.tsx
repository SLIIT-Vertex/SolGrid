import type { ProsumerAccountStatus, ProsumerStatusCounts } from '@/features/prosumers/types'
import { cn } from '@/lib/cn'

interface ProsumerSummaryCardsProps {
  counts: ProsumerStatusCounts | undefined
  isLoading: boolean
  selectedStatus: ProsumerAccountStatus | ''
  onSelect: (status: ProsumerAccountStatus | '') => void
}

const cards: { key: ProsumerAccountStatus; label: string }[] = [
  { key: 'Pending', label: 'Pending activation' },
  { key: 'Active', label: 'Active' },
  { key: 'DeactivationRequested', label: 'Deactivation requested' },
  { key: 'Deactivated', label: 'Deactivated' },
]

export function ProsumerSummaryCards({
  counts,
  isLoading,
  selectedStatus,
  onSelect,
}: ProsumerSummaryCardsProps) {
  return (
    <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
      {cards.map((card) => {
        const isSelected = selectedStatus === card.key
        return (
          <button
            key={card.key}
            type="button"
            onClick={() => onSelect(isSelected ? '' : card.key)}
            className={cn(
              'rounded-2xl border bg-white p-5 text-left transition-colors',
              isSelected ? 'border-brand-300 ring-1 ring-brand-200' : 'border-ink-100 hover:border-ink-200',
            )}
          >
            <p className="text-sm text-ink-500">{card.label}</p>
            <p className="mt-1.5 text-2xl font-semibold text-ink-900">
              {isLoading ? '—' : (counts?.[card.key] ?? 0)}
            </p>
          </button>
        )
      })}
    </div>
  )
}
