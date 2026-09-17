import { formatClock, splitDateTimeOffset } from '@/features/microgrid/format'
import {
  MAX_ADDRESS_LENGTH,
  MAX_CODE_LENGTH,
  MAX_NAME_LENGTH,
  MAX_SLOTS_PER_STATION,
} from '@/features/microgrid/types'
import type {
  BatterySlotFormValues,
  CreateBookingSlotRequest,
  CreateSolarStationRequest,
  MicrogridBatterySlot,
  MicrogridNode,
  MicrogridNodeFormValues,
  OperatingWindow,
  OperatingWindowRequest,
  ScheduleFormValues,
  ScheduleWindowFormValues,
  UpdateBookingSlotRequest,
  UpdateSolarStationRequest,
  UpdateStationScheduleRequest,
} from '@/features/microgrid/types'

export function requiredTrimmed(value: string | undefined, message: string): string | undefined {
  if (!value || !value.trim()) return message
  return undefined
}

export function validateCode(value: string): string | undefined {
  const empty = requiredTrimmed(value, 'Station code is required.')
  if (empty) return empty
  if (value.trim().length > MAX_CODE_LENGTH) {
    return `Station code must not exceed ${MAX_CODE_LENGTH} characters.`
  }
  return undefined
}

export function validateName(value: string): string | undefined {
  const empty = requiredTrimmed(value, 'Station name is required.')
  if (empty) return empty
  if (value.trim().length > MAX_NAME_LENGTH) {
    return `Station name must not exceed ${MAX_NAME_LENGTH} characters.`
  }
  return undefined
}

export function validateAddress(value: string): string | undefined {
  const empty = requiredTrimmed(value, 'Station address is required.')
  if (empty) return empty
  if (value.trim().length > MAX_ADDRESS_LENGTH) {
    return `Station address must not exceed ${MAX_ADDRESS_LENGTH} characters.`
  }
  return undefined
}

export function validateLatitude(value: string): string | undefined {
  const parsed = Number(value)
  if (value.trim() === '' || !Number.isFinite(parsed)) {
    return 'Latitude must be between -90 and 90 degrees.'
  }
  if (parsed < -90 || parsed > 90) {
    return 'Latitude must be between -90 and 90 degrees.'
  }
  return undefined
}

export function validateLongitude(value: string): string | undefined {
  const parsed = Number(value)
  if (value.trim() === '' || !Number.isFinite(parsed)) {
    return 'Longitude must be between -180 and 180 degrees.'
  }
  if (parsed < -180 || parsed > 180) {
    return 'Longitude must be between -180 and 180 degrees.'
  }
  return undefined
}

export function validatePositiveCapacity(value: string, message: string): string | undefined {
  const parsed = Number(value)
  if (value.trim() === '' || !Number.isFinite(parsed) || parsed <= 0) {
    return message
  }
  return undefined
}

export function toTimeOnly(value: string): string {
  const trimmed = value.trim()
  if (/^\d{2}:\d{2}:\d{2}$/.test(trimmed)) return trimmed
  if (/^\d{2}:\d{2}$/.test(trimmed)) return `${trimmed}:00`
  return trimmed
}

function clockToSeconds(value: string): number | null {
  const match = toTimeOnly(value).match(/^(\d{2}):(\d{2}):(\d{2})$/)
  if (!match) return null
  const hours = Number(match[1])
  const minutes = Number(match[2])
  const seconds = Number(match[3])
  if (hours > 23 || minutes > 59 || seconds > 59) return null
  return hours * 3600 + minutes * 60 + seconds
}

export function validateScheduleWindows(windows: ScheduleWindowFormValues[] | undefined): string | undefined {
  if (!windows || windows.length === 0) {
    return 'At least one operating window is required.'
  }

  for (const window of windows) {
    if (!Number.isInteger(window.day) || window.day < 0 || window.day > 6) {
      return 'Operating window day of week is not supported.'
    }

    const opens = clockToSeconds(window.opensAt)
    const closes = clockToSeconds(window.closesAt)
    if (opens === null || closes === null || closes <= opens) {
      return 'An operating window must close after it opens.'
    }
  }

  const ordered = windows
    .map((window) => ({
      day: window.day,
      opens: clockToSeconds(window.opensAt) ?? 0,
      closes: clockToSeconds(window.closesAt) ?? 0,
    }))
    .sort((left, right) => left.day - right.day || left.opens - right.opens)

  for (let index = 1; index < ordered.length; index += 1) {
    const previous = ordered[index - 1]
    const current = ordered[index]
    if (previous.day === current.day && current.opens < previous.closes) {
      return 'Operating windows for the same day must not overlap.'
    }
  }

  return undefined
}

/**
 * Combines a date-picker date and time-picker time — both entered in the Backoffice user's local
 * wall-clock time — into an absolute timestamp for booking slot start/end, keeping the local UTC
 * offset in the output (e.g. `2026-09-17T14:00:00+05:30`) instead of converting to `Z`. The backend
 * validates a slot against the station's operating schedule by reading the clock digits of the
 * *supplied offset* directly (see docs/microgrid-node-management.md: "Operating-schedule checks use
 * the weekday/time of the supplied offset-aware instant") — there is no station timezone field, so
 * converting to UTC here would desync the slot's clock digits from the schedule's local clock digits
 * and make in-hours slots fail validation. This is distinct from `toTimeOnly`, which serializes the
 * weekly operating schedule as a timezone-less clock time by design and must not be touched here.
 */
export function combineDateAndTime(date: string, time: string): string | undefined {
  const trimmedDate = date.trim()
  const trimmedTime = time.trim()
  if (!trimmedDate || !trimmedTime) return undefined

  const [year, month, day] = trimmedDate.split('-').map(Number)
  const timeMatch = toTimeOnly(trimmedTime).match(/^(\d{2}):(\d{2}):(\d{2})$/)
  if (!timeMatch || !Number.isFinite(year) || !Number.isFinite(month) || !Number.isFinite(day)) {
    return undefined
  }
  const [, hours, minutes, seconds] = timeMatch.map(Number)

  const local = new Date(year, month - 1, day, hours, minutes, seconds)
  if (Number.isNaN(local.getTime())) return undefined

  return toIsoStringWithLocalOffset(local)
}

/** Formats a Date as an ISO-8601 string using its local UTC offset instead of shifting to `Z`. */
function toIsoStringWithLocalOffset(value: Date): string {
  const pad = (n: number, width = 2) => String(Math.abs(n)).padStart(width, '0')

  const offsetMinutesTotal = -value.getTimezoneOffset()
  const sign = offsetMinutesTotal >= 0 ? '+' : '-'
  const offsetHours = pad(Math.trunc(offsetMinutesTotal / 60))
  const offsetMinutes = pad(offsetMinutesTotal % 60)

  return (
    `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}` +
    `T${pad(value.getHours())}:${pad(value.getMinutes())}:${pad(value.getSeconds())}` +
    `.${pad(value.getMilliseconds(), 3)}${sign}${offsetHours}:${offsetMinutes}`
  )
}

export function validateSlotInterval(startTime: string, endTime: string): string | undefined {
  const start = Date.parse(startTime)
  const end = Date.parse(endTime)
  if (!Number.isFinite(start) || !Number.isFinite(end) || end <= start) {
    return 'A booking slot must end after it starts.'
  }
  return undefined
}

export function validateUniqueSlotNumbers(slots: BatterySlotFormValues[]): string | undefined {
  const numbers = slots.map((slot) => Number(slot.slotNumber)).filter((value) => Number.isFinite(value))
  if (numbers.length !== new Set(numbers).size) {
    return 'Battery storage slot numbers must be unique within a station.'
  }
  return undefined
}

function isDayPortionCovered(
  schedule: OperatingWindowRequest[],
  day: number,
  fromSeconds: number,
  toSeconds: number,
  coversUntilEndOfDay: boolean,
): boolean {
  if (!coversUntilEndOfDay && fromSeconds >= toSeconds) {
    return true
  }

  const windows = schedule
    .filter((window) => window.day === day)
    .map((window) => ({
      opens: clockToSeconds(window.opensAt) ?? 0,
      closes: clockToSeconds(window.closesAt) ?? 0,
    }))
    .sort((left, right) => left.opens - right.opens)

  let cursor = fromSeconds

  for (const window of windows) {
    if (window.opens > cursor) {
      return false
    }
    if (window.closes > cursor) {
      cursor = window.closes
    }
    if (!coversUntilEndOfDay && cursor >= toSeconds) {
      return true
    }
  }

  return !coversUntilEndOfDay && cursor >= toSeconds
}

/**
 * Reads the wall-clock digits of an offset-aware ISO string as encoded in its own offset — not
 * shifted to UTC. The backend compares a slot's schedule coverage using .NET `DateTimeOffset`'s
 * `TimeOfDay`/`DayOfWeek`, which reflect the value's own offset (there is no station timezone
 * field). `Date.getUTC*` would instead always normalize to UTC regardless of the string's offset,
 * desyncing this client-side pre-check from what the server actually validates.
 */
function readOffsetLocalParts(
  isoString: string,
): { year: number; month: number; day: number; seconds: number } | undefined {
  const match = isoString.match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})$/)
  if (!match) return undefined

  const [, year, month, day, hours, minutes, secs] = match.map(Number)
  return { year, month, day, seconds: hours * 3600 + minutes * 60 + secs }
}

/** Day count since the epoch for an offset-local calendar date, for day-boundary arithmetic. */
function offsetLocalDayIndex(parts: { year: number; month: number; day: number }): number {
  return Math.floor(Date.UTC(parts.year, parts.month - 1, parts.day) / 86_400_000)
}

export function validateSlotAgainstSchedule(
  schedule: ScheduleWindowFormValues[],
  startTime: string,
  endTime: string,
): string | undefined {
  const rangeError = validateSlotInterval(startTime, endTime)
  if (rangeError) return undefined

  const operatingWindows: OperatingWindowRequest[] = schedule.map((window) => ({
    day: window.day,
    opensAt: toTimeOnly(window.opensAt),
    closesAt: toTimeOnly(window.closesAt),
  }))

  const startParts = readOffsetLocalParts(startTime)
  const endParts = readOffsetLocalParts(endTime)
  if (!startParts || !endParts) return undefined

  const startDayIndex = offsetLocalDayIndex(startParts)
  const endDayIndex = offsetLocalDayIndex(endParts)
  const totalSecondsSpan = (endDayIndex - startDayIndex) * 86400 + endParts.seconds - startParts.seconds

  let elapsedSeconds = 0
  let dayIndex = startDayIndex
  let fromSeconds = startParts.seconds

  while (elapsedSeconds < totalSecondsSpan) {
    const secondsLeftInDay = 86400 - fromSeconds
    const secondsRemaining = totalSecondsSpan - elapsedSeconds
    const coversUntilEndOfDay = secondsRemaining >= secondsLeftInDay
    const toSeconds = coversUntilEndOfDay ? 86400 : fromSeconds + secondsRemaining

    // JS `Date.UTC`'s weekday matches the offset-local calendar date directly, since dayIndex was
    // derived from offset-local year/month/day rather than a UTC-shifted instant.
    const dayOfWeek = new Date(dayIndex * 86_400_000).getUTCDay()

    if (!isDayPortionCovered(operatingWindows, dayOfWeek, fromSeconds, toSeconds, coversUntilEndOfDay)) {
      return 'Booking slot times must fall within the station operating schedule.'
    }

    elapsedSeconds += secondsLeftInDay
    dayIndex += 1
    fromSeconds = 0
  }

  return undefined
}

export function validateSlotRow(
  slot: BatterySlotFormValues,
  schedule: ScheduleWindowFormValues[],
): string | undefined {
  const slotNumber = Number(slot.slotNumber)
  if (!Number.isInteger(slotNumber) || slotNumber <= 0) {
    return 'Battery storage slot number must be greater than zero.'
  }

  const capacityError = validatePositiveCapacity(
    slot.batteryCapacityKwh,
    'Battery storage slot capacity in kWh must be greater than zero.',
  )
  if (capacityError) return capacityError

  const startTime = combineDateAndTime(slot.startDate, slot.startTime)
  const endTime = combineDateAndTime(slot.endDate, slot.endTime)
  if (!startTime || !endTime) {
    return 'A booking slot must end after it starts.'
  }

  const intervalError = validateSlotInterval(startTime, endTime)
  if (intervalError) return intervalError

  return validateSlotAgainstSchedule(schedule, startTime, endTime)
}

export function validateInitialSlots(
  slots: BatterySlotFormValues[],
  schedule: ScheduleWindowFormValues[],
): string | undefined {
  if (slots.length > MAX_SLOTS_PER_STATION) {
    return `A station must not declare more than ${MAX_SLOTS_PER_STATION} battery storage slots.`
  }

  const uniqueError = validateUniqueSlotNumbers(slots)
  if (uniqueError) return uniqueError

  for (const slot of slots) {
    const rowError = validateSlotRow(slot, schedule)
    if (rowError) return rowError
  }

  return undefined
}

export function toCreateStationRequest(values: MicrogridNodeFormValues): CreateSolarStationRequest {
  return {
    code: values.code.trim(),
    name: values.name.trim(),
    addressLine: values.addressLine.trim(),
    location: {
      latitude: Number(values.latitude),
      longitude: Number(values.longitude),
    },
    capacityKw: Number(values.capacityKw),
    schedule: values.schedule.map((window) => ({
      day: window.day,
      opensAt: toTimeOnly(window.opensAt),
      closesAt: toTimeOnly(window.closesAt),
    })),
    slots: values.slots.map((slot) => ({
      slotNumber: Number(slot.slotNumber),
      batteryCapacityKwh: Number(slot.batteryCapacityKwh),
      startTime: combineDateAndTime(slot.startDate, slot.startTime) ?? '',
      endTime: combineDateAndTime(slot.endDate, slot.endTime) ?? '',
    })),
  }
}

export function toUpdateStationRequest(values: MicrogridNodeFormValues): UpdateSolarStationRequest {
  return {
    code: values.code.trim(),
    name: values.name.trim(),
    addressLine: values.addressLine.trim(),
    location: {
      latitude: Number(values.latitude),
      longitude: Number(values.longitude),
    },
    capacityKw: Number(values.capacityKw),
  }
}

export function toNodeFormValues(node: MicrogridNode): MicrogridNodeFormValues {
  return {
    code: node.code,
    name: node.name,
    addressLine: node.addressLine,
    latitude: String(node.latitude),
    longitude: String(node.longitude),
    capacityKw: String(node.capacityKw),
    schedule: [],
    slots: [],
  }
}

export function toScheduleFormValues(windows: OperatingWindow[]): ScheduleFormValues {
  return {
    schedule: [...windows]
      .sort((left, right) => left.day - right.day || left.opensAt.localeCompare(right.opensAt))
      .map((window) => ({
        day: window.day,
        opensAt: formatClock(window.opensAt),
        closesAt: formatClock(window.closesAt),
      })),
  }
}

export function toUpdateScheduleRequest(values: ScheduleFormValues): UpdateStationScheduleRequest {
  return {
    schedule: values.schedule.map((window) => ({
      day: window.day,
      opensAt: toTimeOnly(window.opensAt),
      closesAt: toTimeOnly(window.closesAt),
    })),
  }
}

export function toScheduleWindows(windows: OperatingWindow[]): ScheduleWindowFormValues[] {
  return toScheduleFormValues(windows).schedule
}

export const defaultBatterySlotFormValues: BatterySlotFormValues = {
  slotNumber: '',
  batteryCapacityKwh: '',
  startDate: '',
  startTime: '08:00',
  endDate: '',
  endTime: '10:00',
}

export function toSlotFormValues(slot: MicrogridBatterySlot): BatterySlotFormValues {
  const start = splitDateTimeOffset(slot.startTime)
  const end = splitDateTimeOffset(slot.endTime)
  return {
    slotNumber: String(slot.slotNumber),
    batteryCapacityKwh: String(slot.batteryCapacityKwh),
    startDate: start.date,
    startTime: start.time,
    endDate: end.date,
    endTime: end.time,
  }
}

export function toCreateSlotRequest(values: BatterySlotFormValues): CreateBookingSlotRequest {
  return {
    slotNumber: Number(values.slotNumber),
    batteryCapacityKwh: Number(values.batteryCapacityKwh),
    startTime: combineDateAndTime(values.startDate, values.startTime) ?? '',
    endTime: combineDateAndTime(values.endDate, values.endTime) ?? '',
  }
}

export function toUpdateSlotRequest(values: BatterySlotFormValues): UpdateBookingSlotRequest {
  return {
    batteryCapacityKwh: Number(values.batteryCapacityKwh),
    startTime: combineDateAndTime(values.startDate, values.startTime) ?? '',
    endTime: combineDateAndTime(values.endDate, values.endTime) ?? '',
  }
}

export function validateSlotQueryRange(fromDate: string, toDate: string): string | undefined {
  if (!fromDate || !toDate) return undefined
  if (toDate < fromDate) return 'Query end must be after its start.'
  return undefined
}
