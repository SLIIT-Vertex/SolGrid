import { useState } from 'react'
import type { ScheduleWindowFormValues } from '@/features/microgrid/types'

interface SlotDateCalendarProps {
  value: string
  onSelect: (date: string) => void
  schedule: ScheduleWindowFormValues[]
  label: string
  error?: string
  /** An earlier date this calendar's selection may not precede (e.g. an end-date calendar is
   * bounded by the already-picked start date, on top of the general "no past dates" rule). */
  minDate?: string
}

const MONTH_FORMATTER = new Intl.DateTimeFormat(undefined, { month: 'long', year: 'numeric' })
const WEEKDAY_LABELS = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa']

function toDateKey(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function parseDateKey(value: string): Date | undefined {
  const match = value.match(/^(\d{4})-(\d{2})-(\d{2})$/)
  if (!match) return undefined
  const [, year, month, day] = match.map(Number)
  return new Date(year, month - 1, day)
}

function startOfToday(): Date {
  const now = new Date()
  return new Date(now.getFullYear(), now.getMonth(), now.getDate())
}

/**
 * A month-grid date picker for battery slot start/end dates, used instead of a native
 * `<input type="date">` — a native date input's calendar popup is rendered by the OS/browser and
 * cannot be styled per-day, so it can't show which days are actually bookable. This component
 * walks the station's weekly operating schedule to grey out weekdays with no operating window at
 * all (every date falling on that weekday, in every month) and disables past dates outright. A
 * weekday that does have some window may still fail once a specific time is chosen — that full
 * time-range check happens later via validateSlotAgainstSchedule/validateSlotRow, which this
 * calendar does not duplicate since it only ever picks a date, not a date+time combination.
 */
export function SlotDateCalendar({ value, onSelect, schedule, label, error, minDate }: SlotDateCalendarProps) {
  const selected = parseDateKey(value)
  const [visibleMonth, setVisibleMonth] = useState(() => {
    const base = selected ?? parseDateKey(minDate ?? '') ?? new Date()
    return new Date(base.getFullYear(), base.getMonth(), 1)
  })

  const today = startOfToday()
  const earliestAllowed = (() => {
    const boundary = parseDateKey(minDate ?? '')
    if (!boundary) return today
    return boundary > today ? boundary : today
  })()

  const openWeekdays = new Set(schedule.map((window) => window.day))

  const firstOfMonth = new Date(visibleMonth.getFullYear(), visibleMonth.getMonth(), 1)
  const daysInMonth = new Date(visibleMonth.getFullYear(), visibleMonth.getMonth() + 1, 0).getDate()
  const leadingBlanks = firstOfMonth.getDay()
  const cells: (Date | null)[] = [
    ...Array.from({ length: leadingBlanks }, () => null),
    ...Array.from({ length: daysInMonth }, (_, index) => new Date(visibleMonth.getFullYear(), visibleMonth.getMonth(), index + 1)),
  ]

  function goToMonth(offset: number) {
    setVisibleMonth((current) => new Date(current.getFullYear(), current.getMonth() + offset, 1))
  }

  return (
    <div className="flex flex-col gap-1.5">
      <p className="text-sm font-medium text-ink-700">{label}</p>
      <div className="rounded-lg border border-ink-200 bg-white p-3">
        <div className="mb-2 flex items-center justify-between">
          <button
            type="button"
            onClick={() => goToMonth(-1)}
            className="rounded-md px-2 py-1 text-sm text-ink-500 hover:bg-ink-50"
            aria-label="Previous month"
          >
            ‹
          </button>
          <p className="text-sm font-medium text-ink-800">{MONTH_FORMATTER.format(visibleMonth)}</p>
          <button
            type="button"
            onClick={() => goToMonth(1)}
            className="rounded-md px-2 py-1 text-sm text-ink-500 hover:bg-ink-50"
            aria-label="Next month"
          >
            ›
          </button>
        </div>

        <div className="grid grid-cols-7 gap-1 text-center">
          {WEEKDAY_LABELS.map((weekday) => (
            <span key={weekday} className="text-xs font-medium uppercase text-ink-400">
              {weekday}
            </span>
          ))}

          {cells.map((date, index) => {
            if (!date) return <span key={`blank-${index}`} />

            const dateKey = toDateKey(date)
            const isPast = date < earliestAllowed
            const hasNoOperatingWindow = !openWeekdays.has(date.getDay())
            const isDisabled = isPast || hasNoOperatingWindow
            const isSelected = dateKey === value

            return (
              <button
                key={dateKey}
                type="button"
                disabled={isDisabled}
                onClick={() => onSelect(dateKey)}
                title={hasNoOperatingWindow && !isPast ? 'Station is not open on this day of the week.' : undefined}
                className={[
                  'rounded-md py-1.5 text-sm transition-colors',
                  isSelected
                    ? 'bg-brand-600 font-semibold text-white'
                    : isDisabled
                      ? 'cursor-not-allowed text-red-300 line-through'
                      : 'text-ink-700 hover:bg-brand-50',
                ].join(' ')}
              >
                {date.getDate()}
              </button>
            )
          })}
        </div>
      </div>
      {error ? <p className="text-sm text-red-600">{error}</p> : null}
    </div>
  )
}
