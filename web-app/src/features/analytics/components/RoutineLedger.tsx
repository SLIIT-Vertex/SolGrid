import { StatusBadge } from '@/components/common/StatusBadge'
import { DonutChart } from '@/features/analytics/components/charts'
import { chartColors } from '@/features/analytics/palette'
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
  const ordered = [...routines].sort((a, b) => rank(a.outcome) - rank(b.outcome))

  return (
    <div className="space-y-6">
      <section className="rounded-2xl border border-ink-100 bg-white p-5">
        <div className="mb-4">
          <h3 className="text-sm font-semibold text-ink-900">Rule health</h3>
          <p className="mt-0.5 text-xs text-ink-500">Every assignment rule checked against the records stored now.</p>
        </div>
        <DonutChart
          centerValue={routines.length}
          centerLabel="rules"
          slices={[
            { label: 'Clear', value: clear, color: chartColors.brand },
            { label: 'Need attention', value: attention, color: chartColors.amberSoft },
            { label: 'Breaches', value: breaches, color: chartColors.red },
          ]}
        />
      </section>

      <section className="space-y-3">
        <h2 className="text-sm font-semibold text-ink-900">Business routines</h2>
        <div className="overflow-hidden rounded-2xl border border-ink-100 bg-white">
          <ul className="divide-y divide-ink-100">
            {ordered.map((routine) => (
              <li
                key={routine.code}
                className="grid gap-2 px-4 py-4 sm:grid-cols-[minmax(0,16rem)_auto_minmax(0,1fr)] sm:items-start sm:gap-4"
              >
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
    </div>
  )
}

function rank(outcome: RoutineOutcome): number {
  // Surface breaches first, then attention, then clear rules.
  if (outcome === 'Breach') return 0
  if (outcome === 'Attention') return 1
  return 2
}
