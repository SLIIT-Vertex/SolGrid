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

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

export function formatCoordinate(value: number): string {
  if (!Number.isFinite(value)) return '—'
  return String(Number(value.toFixed(6)))
}

export function formatDateTime(value: string): string {
  const match = value.match(/^(\d{4})-(\d{2})-(\d{2})[T ](\d{2}):(\d{2})/)
  if (match) {
    const month = MONTHS[Number(match[2]) - 1]
    if (!month) return value
    return `${Number(match[3])} ${month} ${match[1]}, ${match[4]}:${match[5]}`
  }

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
  const startMatch = start.match(/^(\d{4})-(\d{2})-(\d{2})[T ](\d{2}):(\d{2})/)
  const endMatch = end.match(/^(\d{4})-(\d{2})-(\d{2})[T ](\d{2}):(\d{2})/)
  if (
    startMatch &&
    endMatch &&
    startMatch[1] === endMatch[1] &&
    startMatch[2] === endMatch[2] &&
    startMatch[3] === endMatch[3]
  ) {
    return `${formatDateTime(start)} – ${endMatch[4]}:${endMatch[5]}`
  }

  return `${formatDateTime(start)} – ${formatDateTime(end)}`
}

export function splitDateTimeOffset(value: string): { date: string; time: string } {
  const match = value.match(/^(\d{4}-\d{2}-\d{2})[T ](\d{2}:\d{2})/)
  if (match) return { date: match[1], time: match[2] }
  return { date: '', time: '' }
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
