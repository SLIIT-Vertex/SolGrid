import type { ProsumerAccountStatus, ProsumerStatusCounts } from '@/features/prosumers/types'
import { cn } from '@/lib/cn'

interface ProsumerSummaryCardsProps {
  counts: ProsumerStatusCounts | undefined
  isLoading: boolean
  selectedStatus: ProsumerAccountStatus | ''
  onSelect: (status: ProsumerAccountStatus | '') => void
}

const cards: { key: ProsumerAccountStatus; label: string; dot: string }[] = [
  { key: 'Pending', label: 'Pending activation', dot: 'bg-amber-500' },
  { key: 'Active', label: 'Active', dot: 'bg-brand-500' },
  { key: 'DeactivationRequested', label: 'Deactivation requested', dot: 'bg-amber-500' },
  { key: 'Deactivated', label: 'Deactivated', dot: 'bg-red-500' },
]

export function ProsumerSummaryCards({
  counts,
  isLoading,
  selectedStatus,
  onSelect,
}: ProsumerSummaryCardsProps) {
  return (
    <div
      role="group"
      className="grid grid-cols-2 overflow-hidden rounded-xl border border-ink-200 bg-white lg:grid-cols-4"
      aria-label="Filter prosumers by account status"
    >
      {cards.map((card) => {
        const isSelected = selectedStatus === card.key
        return (
          <button
            key={card.key}
            type="button"
            aria-pressed={isSelected}
            onClick={() => onSelect(isSelected ? '' : card.key)}
            className={cn(
              'min-w-0 border-ink-100 px-4 py-4 text-left transition-colors focus-visible:z-10 focus-visible:outline focus-visible:outline-2 focus-visible:outline-brand-600 sm:px-5',
              '[&:nth-child(odd)]:border-r [&:nth-child(-n+2)]:border-b lg:border-b-0 lg:border-r lg:last:border-r-0',
              isSelected ? 'bg-brand-50 shadow-[inset_0_-3px_0_0_var(--color-brand-500)]' : 'hover:bg-ink-50',
            )}
          >
            <p className="flex min-h-8 items-start gap-2 text-xs font-medium leading-4 text-ink-600 sm:text-sm">
              <span className={cn('mt-1 size-2 shrink-0 rounded-full', card.dot)} aria-hidden="true" />
              {card.label}
            </p>
            <p className="mt-2 text-2xl font-semibold tabular-nums text-ink-900">
              {isLoading ? (
                <span className="inline-block h-7 w-9 animate-pulse rounded bg-ink-100" aria-label="Loading count" />
              ) : (
                (counts?.[card.key] ?? 0).toLocaleString()
              )}
            </p>
          </button>
        )
      })}
    </div>
  )
}
