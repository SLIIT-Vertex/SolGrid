import type { MicrogridBatterySlot } from '@/features/microgrid/types'
import type { Reservation } from './types'

export const MIN_CHANGE_NOTICE_MS = 12 * 60 * 60 * 1000
export const MAX_BOOKING_ADVANCE_MS = 7 * 24 * 60 * 60 * 1000

export function canChangeReservation(
  reservation: Pick<Reservation, 'status' | 'scheduledAt'>,
  now = Date.now(),
) {
  return (
    (reservation.status === 'Pending' || reservation.status === 'Approved') &&
    new Date(reservation.scheduledAt).getTime() - now >= MIN_CHANGE_NOTICE_MS
  )
}

export function scheduleError(value: string, now = Date.now()): string | null {
  const scheduled = new Date(value).getTime()
  if (!value || !Number.isFinite(scheduled))
    return 'Choose a scheduled date and time.'
  if (scheduled <= now) return 'Choose a time in the future.'
  if (scheduled > now + MAX_BOOKING_ADVANCE_MS)
    return 'Choose a time within the next seven days.'
  return null
}

export function slotSelectable(
  slot: Pick<MicrogridBatterySlot, 'id' | 'isActive' | 'isAvailable'>,
  currentSlotId?: string,
) {
  return slot.isActive && (slot.isAvailable || slot.id === currentSlotId)
}
