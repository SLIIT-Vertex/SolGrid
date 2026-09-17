import { describe, expect, it } from 'vitest'
import {
  MAX_ADDRESS_LENGTH,
  MAX_CODE_LENGTH,
  MAX_NAME_LENGTH,
  MAX_SLOTS_PER_STATION,
} from '@/features/microgrid/types'
import type { BatterySlotFormValues, MicrogridNodeFormValues, ScheduleWindowFormValues } from '@/features/microgrid/types'
import {
  combineDateAndTime,
  toCreateStationRequest,
  toTimeOnly,
  toUpdateStationRequest,
  validateAddress,
  validateCode,
  validateInitialSlots,
  validateLatitude,
  validateLongitude,
  validateName,
  validatePositiveCapacity,
  validateScheduleWindows,
  validateSlotAgainstSchedule,
  validateSlotInterval,
  validateSlotQueryRange,
  validateUniqueSlotNumbers,
} from '@/features/microgrid/validation'

const mondayHours: ScheduleWindowFormValues = { day: 1, opensAt: '08:00', closesAt: '17:00' }

/** Mirrors combineDateAndTime's offset-preserving format so tests aren't timezone-specific. */
function expectedLocalIso(year: number, monthIndex: number, day: number, hours: number, minutes: number, seconds = 0): string {
  const value = new Date(year, monthIndex, day, hours, minutes, seconds)
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

function slot(overrides: Partial<BatterySlotFormValues> = {}): BatterySlotFormValues {
  return {
    slotNumber: '1',
    batteryCapacityKwh: '12.5',
    startDate: '2026-09-21',
    startTime: '09:00',
    endDate: '2026-09-21',
    endTime: '11:00',
    ...overrides,
  }
}

describe('node identity validation', () => {
  it('requires a trimmed station code within the API limit', () => {
    expect(validateCode('')).toBe('Station code is required.')
    expect(validateCode('   ')).toBe('Station code is required.')
    expect(validateCode('A'.repeat(MAX_CODE_LENGTH + 1))).toBe(
      `Station code must not exceed ${MAX_CODE_LENGTH} characters.`,
    )
    expect(validateCode('SG-COL-01')).toBeUndefined()
  })

  it('requires a station name and address within the API limits', () => {
    expect(validateName('')).toBe('Station name is required.')
    expect(validateName('N'.repeat(MAX_NAME_LENGTH + 1))).toBe(
      `Station name must not exceed ${MAX_NAME_LENGTH} characters.`,
    )
    expect(validateAddress('')).toBe('Station address is required.')
    expect(validateAddress('A'.repeat(MAX_ADDRESS_LENGTH + 1))).toBe(
      `Station address must not exceed ${MAX_ADDRESS_LENGTH} characters.`,
    )
    expect(validateName('Colombo North')).toBeUndefined()
    expect(validateAddress('12 Power Lane')).toBeUndefined()
  })
})

describe('location and capacity validation', () => {
  it('accepts latitudes between -90 and 90', () => {
    expect(validateLatitude('')).toBe('Latitude must be between -90 and 90 degrees.')
    expect(validateLatitude('91')).toBe('Latitude must be between -90 and 90 degrees.')
    expect(validateLatitude('-90')).toBeUndefined()
    expect(validateLatitude('6.9271')).toBeUndefined()
  })

  it('accepts longitudes between -180 and 180', () => {
    expect(validateLongitude('abc')).toBe('Longitude must be between -180 and 180 degrees.')
    expect(validateLongitude('181')).toBe('Longitude must be between -180 and 180 degrees.')
    expect(validateLongitude('79.8612')).toBeUndefined()
  })

  it('requires generation and storage capacity to be greater than zero', () => {
    expect(validatePositiveCapacity('0', 'Station capacity in kW must be greater than zero.')).toBe(
      'Station capacity in kW must be greater than zero.',
    )
    expect(validatePositiveCapacity('12.5', 'Station capacity in kW must be greater than zero.')).toBeUndefined()
  })
})

describe('schedule validation', () => {
  it('requires at least one weekly window that closes after it opens', () => {
    expect(validateScheduleWindows([])).toBe('At least one operating window is required.')
    expect(validateScheduleWindows([{ day: 1, opensAt: '17:00', closesAt: '08:00' }])).toBe(
      'An operating window must close after it opens.',
    )
    expect(validateScheduleWindows([{ day: 8, opensAt: '08:00', closesAt: '17:00' }])).toBe(
      'Operating window day of week is not supported.',
    )
  })

  it('rejects overlapping same-day windows but allows adjacent windows', () => {
    expect(
      validateScheduleWindows([
        { day: 1, opensAt: '08:00', closesAt: '12:00' },
        { day: 1, opensAt: '11:00', closesAt: '15:00' },
      ]),
    ).toBe('Operating windows for the same day must not overlap.')

    expect(
      validateScheduleWindows([
        { day: 1, opensAt: '08:00', closesAt: '12:00' },
        { day: 1, opensAt: '12:00', closesAt: '17:00' },
      ]),
    ).toBeUndefined()
  })
})

describe('battery slot validation', () => {
  it('formats TimeOnly values for the weekly schedule, and converts slot picks to real UTC instants', () => {
    expect(toTimeOnly('08:00')).toBe('08:00:00')
    expect(toTimeOnly('08:00:30')).toBe('08:00:30')

    // combineDateAndTime treats the picked date/time as the browser's local wall-clock time and
    // keeps that local offset in the output — the backend reads schedule coverage from the
    // supplied offset's own clock digits, not from a UTC shift — so this is checked against a
    // real Date construction rather than a timezone-specific literal string.
    expect(combineDateAndTime('2026-09-21', '09:00')).toBe(expectedLocalIso(2026, 8, 21, 9, 0, 0))
  })

  it('requires a booking window that ends after it starts', () => {
    expect(validateSlotInterval('2026-09-21T11:00:00+00:00', '2026-09-21T09:00:00+00:00')).toBe(
      'A booking slot must end after it starts.',
    )
    expect(validateSlotInterval('2026-09-21T09:00:00+00:00', '2026-09-21T11:00:00+00:00')).toBeUndefined()
  })

  it('requires slot numbers to be unique on a node', () => {
    expect(validateUniqueSlotNumbers([slot({ slotNumber: '1' }), slot({ slotNumber: '1' })])).toBe(
      'Battery storage slot numbers must be unique within a station.',
    )
    expect(validateUniqueSlotNumbers([slot({ slotNumber: '1' }), slot({ slotNumber: '2' })])).toBeUndefined()
  })

  it('requires slot times to sit inside the weekly operating hours', () => {
    expect(
      validateSlotAgainstSchedule(
        [mondayHours],
        '2026-09-21T07:00:00+00:00',
        '2026-09-21T09:00:00+00:00',
      ),
    ).toBe('Booking slot times must fall within the station operating schedule.')

    expect(
      validateSlotAgainstSchedule(
        [mondayHours],
        '2026-09-21T09:00:00+00:00',
        '2026-09-21T11:00:00+00:00',
      ),
    ).toBeUndefined()
  })

  it('caps the number of battery slots declared with a station', () => {
    const tooMany = Array.from({ length: MAX_SLOTS_PER_STATION + 1 }, (_, index) =>
      slot({ slotNumber: String(index + 1) }),
    )
    expect(validateInitialSlots(tooMany, [mondayHours])).toBe(
      `A station must not declare more than ${MAX_SLOTS_PER_STATION} battery storage slots.`,
    )
  })

  it('rejects inverted query date ranges', () => {
    expect(validateSlotQueryRange('2026-09-22', '2026-09-21')).toBe('Query end must be after its start.')
    expect(validateSlotQueryRange('2026-09-21', '2026-09-22')).toBeUndefined()
    expect(validateSlotQueryRange('', '2026-09-22')).toBeUndefined()
  })
})

describe('request mapping', () => {
  const values: MicrogridNodeFormValues = {
    code: ' SG-01 ',
    name: ' North Node ',
    addressLine: ' 12 Power Lane ',
    latitude: '6.9271',
    longitude: '79.8612',
    capacityKw: '40',
    schedule: [mondayHours],
    slots: [slot()],
  }

  it('maps create requests with trimmed text, kW generation, kWh storage, and offset-local slot times', () => {
    // Slot start/end are picked in the Backoffice user's local wall-clock time and keep that local
    // offset — computed here rather than hardcoded so the test isn't timezone-specific.
    const expectedStart = expectedLocalIso(2026, 8, 21, 9, 0, 0)
    const expectedEnd = expectedLocalIso(2026, 8, 21, 11, 0, 0)

    expect(toCreateStationRequest(values)).toEqual({
      code: 'SG-01',
      name: 'North Node',
      addressLine: '12 Power Lane',
      location: { latitude: 6.9271, longitude: 79.8612 },
      capacityKw: 40,
      schedule: [{ day: 1, opensAt: '08:00:00', closesAt: '17:00:00' }],
      slots: [
        {
          slotNumber: 1,
          batteryCapacityKwh: 12.5,
          startTime: expectedStart,
          endTime: expectedEnd,
        },
      ],
    })
  })

  it('maps update requests without schedule or slot payloads', () => {
    expect(toUpdateStationRequest(values)).toEqual({
      code: 'SG-01',
      name: 'North Node',
      addressLine: '12 Power Lane',
      location: { latitude: 6.9271, longitude: 79.8612 },
      capacityKw: 40,
    })
  })
})
