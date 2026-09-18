import { Link } from 'react-router-dom'
import { cn } from '@/lib/cn'

interface StatCardProps {
  label: string
  value: number | undefined
  hint?: string
  /** When set the whole card links here. */
  to?: string
  isLoading?: boolean
  isError?: boolean
  /** Highlights work that is waiting on someone. */
  needsAttention?: boolean
}

export function StatCard({ label, value, hint, to, isLoading, isError, needsAttention }: StatCardProps) {
  const isHighlighted = Boolean(needsAttention) && !isLoading && !isError && (value ?? 0) > 0

  const body = (
    <>
      <p className="text-sm text-ink-500">{label}</p>
      <p
        className={cn(
          'mt-1.5 text-2xl font-semibold tabular-nums',
          isHighlighted ? 'text-amber-700' : 'text-ink-900',
        )}
      >
        {isLoading || isError ? '—' : (value ?? 0)}
      </p>
      {isError ? (
        <p className="mt-1 text-xs text-ink-400">Couldn't load this figure.</p>
      ) : hint ? (
        <p className="mt-1 text-xs text-ink-400">{hint}</p>
      ) : null}
    </>
  )

  const className = cn(
    'block rounded-2xl border bg-white p-5',
    isHighlighted ? 'border-amber-200' : 'border-ink-100',
  )

  if (!to) {
    return <div className={className}>{body}</div>
  }

  return (
    <Link
      to={to}
      className={cn(
        className,
        'transition-colors hover:border-brand-200 hover:bg-brand-50/30',
        'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600',
      )}
    >
      {body}
    </Link>
  )
}
