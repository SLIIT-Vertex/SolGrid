import { WEEKDAYS } from '@/features/microgrid/types'
import type { OperatingWindow } from '@/features/microgrid/types'
import { ScheduleClockNote } from '@/features/microgrid/components/ScheduleClockNote'
import { formatWindowsForDay } from '@/features/microgrid/format'

export function ScheduleView({ schedule }: { schedule: OperatingWindow[] }) {
  return (
    <div className="flex flex-col gap-3">
      <ScheduleClockNote />
      <div className="overflow-hidden rounded-xl border border-ink-100">
        {WEEKDAYS.map((weekday) => {
          const hours = formatWindowsForDay(schedule, weekday.day)
          const closed = hours === 'Closed'
          return (
            <div
              key={weekday.day}
              className="flex items-start justify-between gap-3 border-t border-ink-100 px-4 py-3 text-sm first:border-t-0 even:bg-ink-50/60"
            >
              <span className="font-medium text-ink-800">{weekday.label}</span>
              <span className={closed ? 'text-ink-400' : 'text-right text-ink-700'}>{hours}</span>
            </div>
          )
        })}
      </div>
    </div>
  )
}
