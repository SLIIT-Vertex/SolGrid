import { WEEKDAYS } from '@/features/microgrid/types'
import type { OperatingWindow } from '@/features/microgrid/types'

export function formatGenerationKw(capacityKw: number): string {
  return `${capacityKw} kW`
}

export function formatStorageKwh(capacityKwh: number): string {
  return `${capacityKwh} kWh`
}

export function formatSlotCounts(available: number, total: number): string {
  return `${available} / ${total}`
}

export function formatClock(value: string): string {
  return value.length >= 5 ? value.slice(0, 5) : value
}

export function formatCoordinate(value: number): string {
  if (!Number.isFinite(value)) return '—'
  return String(Number(value.toFixed(6)))
}

/**
 * Formats an absolute UTC timestamp (booking slot start/end) in the viewer's local time zone.
 * These values are true instants — unlike the weekly operating schedule's timezone-less
 * `TimeOnly` clock strings (see `formatClock`) — so they must always be converted, never read
 * as literal UTC digits.
 */
export function formatDateTime(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat('en-GB', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(date)
}

export function formatDateTimeRange(start: string, end: string): string {
  const startDate = new Date(start)
  const endDate = new Date(end)
  if (Number.isNaN(startDate.getTime()) || Number.isNaN(endDate.getTime())) {
    return `${formatDateTime(start)} – ${formatDateTime(end)}`
  }

  const sameLocalDay =
    startDate.getFullYear() === endDate.getFullYear() &&
    startDate.getMonth() === endDate.getMonth() &&
    startDate.getDate() === endDate.getDate()

  if (sameLocalDay) {
    const endTime = new Intl.DateTimeFormat('en-GB', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    }).format(endDate)
    return `${formatDateTime(start)} – ${endTime}`
  }

  return `${formatDateTime(start)} – ${formatDateTime(end)}`
}

/** Splits an absolute UTC timestamp into the local date/time an <input type="date"/time"> pair
 * expects, so editing an existing booking slot shows the time it was actually created at. */
export function splitDateTimeOffset(value: string): { date: string; time: string } {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return { date: '', time: '' }

  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  const hours = String(date.getHours()).padStart(2, '0')
  const minutes = String(date.getMinutes()).padStart(2, '0')

  return { date: `${year}-${month}-${day}`, time: `${hours}:${minutes}` }
}

export function formatWindowsForDay(windows: OperatingWindow[], day: number): string {
  const dayWindows = windows
    .filter((window) => window.day === day)
    .sort((left, right) => left.opensAt.localeCompare(right.opensAt))

  if (dayWindows.length === 0) return 'Closed'
  return dayWindows.map((window) => `${formatClock(window.opensAt)} – ${formatClock(window.closesAt)}`).join(', ')
}

export function formatScheduleSummary(windows: OperatingWindow[]): string {
  const openDays = WEEKDAYS.filter((weekday) => windows.some((window) => window.day === weekday.day))
  if (openDays.length === 0) return 'No operating windows'
  return openDays.map((weekday) => `${weekday.label} ${formatWindowsForDay(windows, weekday.day)}`).join(' · ')
}
