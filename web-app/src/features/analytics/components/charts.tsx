/**
 * Small SVG chart primitives drawn from the app's own palette. They avoid a charting dependency
 * so the analytics surface keeps the same component vocabulary as the rest of the console.
 */

export interface Slice {
  label: string
  value: number
  color: string
}

const EMPTY_RING = '#eef0f2'

function polarToXy(cx: number, cy: number, radius: number, fraction: number) {
  const angle = fraction * 2 * Math.PI - Math.PI / 2
  return [cx + radius * Math.cos(angle), cy + radius * Math.sin(angle)]
}

function arcPath(cx: number, cy: number, radius: number, from: number, to: number) {
  const [startX, startY] = polarToXy(cx, cy, radius, from)
  const [endX, endY] = polarToXy(cx, cy, radius, to)
  const largeArc = to - from > 0.5 ? 1 : 0
  return `M ${startX} ${startY} A ${radius} ${radius} 0 ${largeArc} 1 ${endX} ${endY}`
}

/** A donut that reads its center number at a glance and its split from the ring and legend. */
export function DonutChart({
  slices,
  centerValue,
  centerLabel,
}: {
  slices: Slice[]
  centerValue: number | string
  centerLabel: string
}) {
  const total = slices.reduce((sum, slice) => sum + slice.value, 0)
  const size = 168
  const stroke = 20
  const radius = (size - stroke) / 2
  const cx = size / 2
  const cy = size / 2
  let cursor = 0

  return (
    <div className="flex items-center gap-5">
      <svg viewBox={`0 0 ${size} ${size}`} className="size-40 shrink-0" role="img" aria-label={`${centerLabel}: ${centerValue}`}>
        <circle cx={cx} cy={cy} r={radius} fill="none" stroke={EMPTY_RING} strokeWidth={stroke} />
        {total > 0 &&
          slices
            .filter((slice) => slice.value > 0)
            .map((slice) => {
              const from = cursor / total
              cursor += slice.value
              const to = cursor / total
              // A full ring has no arc endpoints, so draw it as a plain circle instead.
              if (slice.value === total) {
                return (
                  <circle
                    key={slice.label}
                    cx={cx}
                    cy={cy}
                    r={radius}
                    fill="none"
                    stroke={slice.color}
                    strokeWidth={stroke}
                  />
                )
              }
              return (
                <path
                  key={slice.label}
                  d={arcPath(cx, cy, radius, from, to)}
                  fill="none"
                  stroke={slice.color}
                  strokeWidth={stroke}
                  strokeLinecap="butt"
                />
              )
            })}
        <text x={cx} y={cy - 2} textAnchor="middle" className="fill-ink-900 text-2xl font-semibold tabular-nums" style={{ fontSize: 30 }}>
          {centerValue}
        </text>
        <text x={cx} y={cy + 20} textAnchor="middle" className="fill-ink-400" style={{ fontSize: 12 }}>
          {centerLabel}
        </text>
      </svg>
      <ul className="min-w-0 flex-1 space-y-1.5">
        {slices.map((slice) => (
          <li key={slice.label} className="flex items-center justify-between gap-3 text-sm">
            <span className="flex min-w-0 items-center gap-2 text-ink-600">
              <span className="size-2.5 shrink-0 rounded-full" style={{ backgroundColor: slice.color }} aria-hidden="true" />
              <span className="truncate">{slice.label}</span>
            </span>
            <span className="tabular-nums text-ink-900">{slice.value}</span>
          </li>
        ))}
      </ul>
    </div>
  )
}

export interface Bar {
  label: string
  value: number
  color?: string
  hint?: string
}

/** A horizontal bar list — better than a stat grid when the reader is comparing magnitudes. */
export function BarChart({ bars, unit }: { bars: Bar[]; unit?: string }) {
  const max = Math.max(1, ...bars.map((bar) => bar.value))

  return (
    <ul className="space-y-3">
      {bars.map((bar) => (
        <li key={bar.label}>
          <div className="flex items-baseline justify-between gap-3 text-sm">
            <span className="truncate text-ink-600">{bar.label}</span>
            <span className="tabular-nums text-ink-900">
              {bar.value}
              {unit ? <span className="ml-0.5 text-ink-400">{unit}</span> : null}
            </span>
          </div>
          <div className="mt-1 h-2 overflow-hidden rounded-full bg-ink-100">
            <div
              className="h-full rounded-full"
              style={{ width: `${Math.round((bar.value / max) * 100)}%`, backgroundColor: bar.color ?? '#139a68' }}
            />
          </div>
          {bar.hint ? <p className="mt-1 text-xs text-ink-400">{bar.hint}</p> : null}
        </li>
      ))}
    </ul>
  )
}

/** A single proportion drawn as a thick meter with its own label — used for utilization. */
export function MeterBar({
  label,
  percent,
  caption,
  color = '#139a68',
}: {
  label: string
  percent: number
  caption?: string
  color?: string
}) {
  const clamped = Math.max(0, Math.min(100, percent))
  return (
    <div>
      <div className="flex items-baseline justify-between gap-3">
        <span className="text-sm text-ink-600">{label}</span>
        <span className="text-sm font-semibold tabular-nums text-ink-900">{clamped}%</span>
      </div>
      <div className="mt-1.5 h-2.5 overflow-hidden rounded-full bg-ink-100">
        <div className="h-full rounded-full" style={{ width: `${clamped}%`, backgroundColor: color }} />
      </div>
      {caption ? <p className="mt-1 text-xs text-ink-400">{caption}</p> : null}
    </div>
  )
}

export function ChartCard({
  title,
  description,
  children,
}: {
  title: string
  description?: string
  children: React.ReactNode
}) {
  return (
    <section className="rounded-2xl border border-ink-100 bg-white p-5">
      <div className="mb-4">
        <h3 className="text-sm font-semibold text-ink-900">{title}</h3>
        {description ? <p className="mt-0.5 text-xs text-ink-500">{description}</p> : null}
      </div>
      {children}
    </section>
  )
}
