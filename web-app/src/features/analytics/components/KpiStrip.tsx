export interface Kpi {
  label: string
  value: string | number
  hint?: string
  tone?: 'default' | 'attention' | 'danger'
}

const toneClass: Record<NonNullable<Kpi['tone']>, string> = {
  default: 'text-ink-900',
  attention: 'text-amber-700',
  danger: 'text-red-700',
}

/** A compact row of headline numbers. One glance, no reading. */
export function KpiStrip({ items }: { items: Kpi[] }) {
  return (
    <dl className="grid grid-cols-2 gap-px overflow-hidden rounded-2xl border border-ink-100 bg-ink-100 sm:grid-cols-3 lg:grid-cols-5">
      {items.map((item) => (
        <div key={item.label} className="bg-white px-4 py-4">
          <dt className="text-xs text-ink-500">{item.label}</dt>
          <dd className={`mt-1.5 text-2xl font-semibold tabular-nums ${toneClass[item.tone ?? 'default']}`}>
            {item.value}
          </dd>
          {item.hint ? <p className="mt-0.5 text-xs text-ink-400">{item.hint}</p> : null}
        </div>
      ))}
    </dl>
  )
}
