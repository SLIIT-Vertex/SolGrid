import { cn } from '@/lib/cn'

type Tone = 'brand' | 'ink' | 'red' | 'amber'

const toneClasses: Record<Tone, string> = {
  brand: 'bg-brand-50 text-brand-700 ring-brand-200',
  ink: 'bg-ink-100 text-ink-600 ring-ink-200',
  red: 'bg-red-50 text-red-700 ring-red-200',
  amber: 'bg-amber-50 text-amber-700 ring-amber-200',
}

export function StatusBadge({ label, tone }: { label: string; tone: Tone }) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset',
        toneClasses[tone],
      )}
    >
      {label}
    </span>
  )
}
