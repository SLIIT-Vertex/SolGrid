import { describe, expect, it } from 'vitest'
import {
  canChangeReservation,
  scheduleError,
  slotSelectable,
} from './bookingRules'
import type { Reservation } from './types'
import type { MicrogridBatterySlot } from '@/features/microgrid/types'

const now = Date.parse('2026-09-19T06:00:00Z')
const reservation: Pick<Reservation, 'status' | 'scheduledAt'> = {
  status: 'Approved',
  scheduledAt: '2026-09-19T18:00:00Z',
}

describe('reservation booking restrictions', () => {
  it('allows exactly twelve hours notice and blocks shorter or terminal bookings', () => {
    expect(canChangeReservation(reservation, now)).toBe(true)
    expect(canChangeReservation(reservation, now + 1)).toBe(false)
    expect(
      canChangeReservation({ ...reservation, status: 'Completed' }, now),
    ).toBe(false)
  })
  it('accepts the seven-day boundary, and rejects missing, invalid, past, and later dates', () => {
    expect(scheduleError('2026-09-26T06:00:00Z', now)).toBeNull()
    expect(scheduleError('2026-09-26T06:00:01Z', now)).toMatch(/seven days/)
    expect(scheduleError('2026-09-19T06:00:00Z', now)).toMatch(/future/)
    expect(scheduleError('', now)).toMatch(/Choose/)
    expect(scheduleError('invalid', now)).toMatch(/Choose/)
  })
  it('shows committed slots without allowing selection, except the current editable slot', () => {
    const slot: Pick<MicrogridBatterySlot, 'id' | 'isActive' | 'isAvailable'> =
      {
        id: 'slot-1',
        isActive: true,
        isAvailable: false,
      }
    expect(slotSelectable(slot)).toBe(false)
    expect(slotSelectable(slot, 'slot-1')).toBe(true)
    expect(slotSelectable({ ...slot, isActive: false }, 'slot-1')).toBe(false)
  })
})
