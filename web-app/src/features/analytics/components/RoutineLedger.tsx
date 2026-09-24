import { StatusBadge } from '@/components/common/StatusBadge'
import type { BusinessRoutine, RoutineOutcome } from '@/features/analytics/types'

const outcomeTone: Record<RoutineOutcome, 'brand' | 'amber' | 'red'> = {
  Clear: 'brand',
  Attention: 'amber',
  Breach: 'red',
}

export function RoutineLedger({ routines }: { routines: BusinessRoutine[] }) {
  const breaches = routines.filter((routine) => routine.outcome === 'Breach').length
  const attention = routines.filter((routine) => routine.outcome === 'Attention').length
  const clear = routines.filter((routine) => routine.outcome === 'Clear').length

  return (
    <section className="space-y-3">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h2 className="text-sm font-semibold text-ink-900">Business routines</h2>
          <p className="text-sm text-ink-500">
            Each assignment rule checked against the records stored now.
          </p>
        </div>
        <p className="text-sm tabular-nums text-ink-600">
          {clear} clear · {attention} need attention · {breaches} breaches
        </p>
      </div>
      <div className="overflow-hidden rounded-2xl border border-ink-100 bg-white">
        <ul className="divide-y divide-ink-100">
          {routines.map((routine) => (
            <li key={routine.code} className="grid gap-2 px-4 py-4 sm:grid-cols-[minmax(0,16rem)_auto_minmax(0,1fr)] sm:items-start sm:gap-4">
              <div>
                <p className="text-sm font-medium text-ink-900">{routine.name}</p>
                <p className="mt-1 text-xs leading-5 text-ink-500">{routine.rule}</p>
              </div>
              <StatusBadge label={routine.outcome} tone={outcomeTone[routine.outcome]} />
              <p className="text-sm leading-6 text-ink-700">{routine.detail}</p>
            </li>
          ))}
        </ul>
      </div>
    </section>
  )
}
