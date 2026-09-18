import { useCallback, useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useToast } from '@/components/common/useToast'
import type { Prosumer } from '@/features/prosumers/types'
import { microgridKeys } from '@/features/microgrid/queryKeys'
import {
  createReservation,
  updateReservation,
} from '@/features/reservations/api'
import type { Reservation } from '@/features/reservations/types'
import { reservationsKeys } from '@/features/reservations/queryKeys'
import {
  canChangeReservation,
  scheduleError,
  slotSelectable,
} from '../bookingRules'
import { localDateTime } from '../presentation'
import { bookingSteps } from '../bookingSteps'
import type { BookingStep } from '../bookingSteps'
import { useReservationBookingOptions } from './useReservationBookingOptions'
export function useReservationBooking(
  reservation: Reservation | undefined,
  onClose: () => void,
  onStepChange: () => void,
) {
  const [openedAt] = useState(() => Date.now())
  const [step, setStep] = useState<BookingStep>(
    reservation ? 'grid-node' : 'prosumer',
  )
  const [person, setPerson] = useState<Prosumer | null>(null)
  const [stationId, setStationId] = useState(reservation?.stationId ?? '')
  const [bookingSlotId, setSlotId] = useState(reservation?.bookingSlotId ?? '')
  const [scheduledAt, setScheduledAt] = useState(
    reservation ? localDateTime(reservation.scheduledAt) : '',
  )
  const [validation, setValidation] = useState<string | null>(null)
  const client = useQueryClient()
  const { showToast } = useToast()
  const {
    person: existingPerson,
    stations,
    selectedStation,
    slots,
    sortedSlots,
    selectedSlot,
  } = useReservationBookingOptions(reservation, stationId, bookingSlotId)
  const selectedPerson = person ?? existingPerson ?? null
  const editable = !reservation || canChangeReservation(reservation, openedAt)
  const slotAvailable =
    !!selectedSlot && slotSelectable(selectedSlot, reservation?.bookingSlotId)
  const stationActive = selectedStation?.status === 'Active'
  const selectStation = useCallback((id: string) => {
    setStationId(id)
    setSlotId('')
    setScheduledAt('')
    setValidation(null)
  }, [])
  const selectSlot = (id: string) => {
    const slot = sortedSlots.find((item) => item.id === id)
    if (!slot) return
    setSlotId(id)
    setScheduledAt(localDateTime(slot.startTime))
    setValidation(null)
  }
  const changeScheduledAt = (value: string) => {
    setScheduledAt(value)
    setValidation(null)
  }
  const save = useMutation({
    mutationFn: () => {
      const request = {
        stationId,
        bookingSlotId,
        scheduledAt: new Date(scheduledAt).toISOString(),
      }
      if (reservation) return updateReservation(reservation.id, request)
      if (!selectedPerson)
        throw new Error('Select a prosumer before creating a reservation.')
      return createReservation({ ...request, prosumerId: selectedPerson.nic })
    },
    onSuccess: async () => {
      await Promise.all([
        client.invalidateQueries({ queryKey: reservationsKeys.all }),
        client.invalidateQueries({ queryKey: microgridKeys.all }),
      ])
      showToast(
        reservation
          ? 'Reservation updated.'
          : 'Reservation created and awaiting approval.',
      )
      onClose()
    },
    onError: () => {
      void slots.refetch()
    },
  })
  const goTo = (next: BookingStep) => {
    if (save.isPending) return
    setStep(next)
    setValidation(null)
    save.reset()
    requestAnimationFrame(onStepChange)
  }
  const continueStep = () => {
    if (step === 'prosumer' && !selectedPerson) {
      setValidation('Select an active prosumer to continue.')
      return
    }
    if (step === 'grid-node' && !stationActive) {
      setValidation('Select an active grid node to continue.')
      return
    }
    if (step === 'slot-time') {
      if (!slotAvailable) {
        setValidation('Select an available battery slot.')
        return
      }
      const error = scheduleError(scheduledAt)
      if (error) {
        setValidation(error)
        return
      }
    }
    const next = bookingSteps[step].next
    if (next) goTo(next)
  }
  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (save.isPending) return
    if (step !== 'review') {
      continueStep()
      return
    }
    const error = scheduleError(scheduledAt)
    if (
      (reservation && !canChangeReservation(reservation)) ||
      !stationActive ||
      !slotAvailable ||
      (!reservation && selectedPerson?.status !== 'Active') ||
      error
    ) {
      setValidation(
        error ??
          'This booking is no longer available. Go back and check your selections.',
      )
      return
    }
    save.mutate()
  }
  const slotReady =
    !!selectedSlot && !!scheduledAt && !slots.isFetching && !slots.isError
  const selectionReady: Record<BookingStep, boolean> = {
    prosumer: !!selectedPerson,
    'grid-node': stationActive,
    'slot-time': slotReady,
    review: slotReady,
  }
  const canContinue = editable && selectionReady[step]

  return {
    openedAt,
    step,
    person: selectedPerson,
    setPerson,
    bookingSlotId,
    scheduledAt,
    changeScheduledAt,
    selectSlot,
    validation,
    stations,
    selectedStation,
    slots,
    sortedSlots,
    selectedSlot,
    editable,
    stationActive,
    selectStation,
    save,
    goTo,
    submit,
    canContinue,
  }
}
