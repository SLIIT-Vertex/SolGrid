import { useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button } from '@/components/common/Button'
import { Dialog } from '@/components/common/Dialog'
import { TextField } from '@/components/common/TextField'
import { SelectField } from '@/components/common/SelectField'
import { useToast } from '@/components/common/useToast'
import { listStations, listSlots } from '@/features/microgrid/api'
import { defaultMicrogridNodeFilters, defaultMicrogridSlotFilters } from '@/features/microgrid/types'
import { microgridKeys } from '@/features/microgrid/queryKeys'
import { createReservation, updateReservation } from '@/features/reservations/api'
import type { Reservation } from '@/features/reservations/types'
import { reservationsKeys } from '@/features/reservations/queryKeys'
import { getErrorMessage } from '@/lib/problemDetails'
import { PROSUMER_NIC_MAX_LENGTH, sanitizeNicInput } from '@/features/prosumers/validation'

async function allPages<T>(load: (page: number) => Promise<{ items: T[]; totalCount: number }>) {
  const items: T[] = []
  for (let page = 1; ; page++) {
    const result = await load(page)
    items.push(...result.items)
    if (!result.items.length || items.length >= result.totalCount) return items
  }
}
function localDate(iso: string) {
  const date = new Date(iso)
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16)
}
export function ReservationDialog({ reservation, onClose }: { reservation?: Reservation; onClose: () => void }) {
  const [prosumerId, setProsumerId] = useState(reservation?.prosumerId ?? '')
  const [stationId, setStationId] = useState(reservation?.stationId ?? '')
  const [bookingSlotId, setSlotId] = useState(reservation?.bookingSlotId ?? '')
  const [scheduledAt, setScheduledAt] = useState(reservation ? localDate(reservation.scheduledAt) : '')
  const client = useQueryClient()
  const { showToast } = useToast()
  const stations = useQuery({ queryKey: ['reservation-form', 'stations'], queryFn: () => allPages((pageNumber) => listStations({ ...defaultMicrogridNodeFilters, status: 'Active', pageNumber })) })
  const slots = useQuery({ queryKey: ['reservation-form', 'slots', stationId], enabled: !!stationId, queryFn: () => allPages((pageNumber) => listSlots(stationId, { ...defaultMicrogridSlotFilters, pageNumber })) })
  const availableSlots = (slots.data ?? []).filter((slot) => slot.isAvailable || slot.id === reservation?.bookingSlotId)
  const save = useMutation({
    mutationFn: () => {
      const request = { stationId, bookingSlotId, scheduledAt: new Date(scheduledAt).toISOString() }
      return reservation ? updateReservation(reservation.id, request) : createReservation({ ...request, prosumerId })
    },
    onSuccess: async () => {
      await Promise.all([client.invalidateQueries({ queryKey: reservationsKeys.all }), client.invalidateQueries({ queryKey: microgridKeys.all })])
      showToast(reservation ? 'Reservation updated.' : 'Reservation created and awaiting approval.')
      onClose()
    },
  })
  const submit = (event: FormEvent) => { event.preventDefault(); save.mutate() }
  const referenceError = stations.error ?? slots.error
  return <Dialog open title={reservation ? 'Edit reservation' : 'Create reservation'} onClose={() => { if (!save.isPending) onClose() }} size="lg">
    <form onSubmit={submit} className="grid gap-4">
      <TextField name="prosumerNic" label="Prosumer NIC" required readOnly={!!reservation} value={prosumerId} maxLength={PROSUMER_NIC_MAX_LENGTH} sanitizeValue={sanitizeNicInput} onChange={(event) => setProsumerId(event.target.value)} />
      <SelectField name="station" label="Grid node" required value={stationId} onChange={(event) => { setStationId(event.target.value); setSlotId('') }}><option value="">Select a node</option>{stations.data?.map((station) => <option key={station.id} value={station.id}>{station.name}</option>)}</SelectField>
      <SelectField name="slot" label="Battery slot" required value={bookingSlotId} onChange={(event) => { setSlotId(event.target.value); const slot = availableSlots.find((item) => item.id === event.target.value); if (slot) setScheduledAt(localDate(slot.startTime)) }}><option value="">Select a slot</option>{availableSlots.map((slot) => <option key={slot.id} value={slot.id}>#{slot.slotNumber} · {slot.batteryCapacityKwh} kWh · {new Date(slot.startTime).toLocaleString()} – {new Date(slot.endTime).toLocaleString()}</option>)}</SelectField>
      <TextField name="scheduledAt" label="Scheduled time" type="datetime-local" required value={scheduledAt} onChange={(event) => setScheduledAt(event.target.value)} />
      <p className="text-sm text-ink-500">Book within seven days. Changes require at least twelve hours’ notice. Times use your device timezone.</p>
      {(save.isError || referenceError) && <p role="alert" className="text-sm text-red-600">{getErrorMessage(save.error ?? referenceError)}</p>}
      {referenceError && <Button type="button" variant="secondary" onClick={() => { void stations.refetch(); void slots.refetch() }}>Retry loading nodes and slots</Button>}
      <div className="flex justify-end gap-2"><Button type="button" variant="secondary" disabled={save.isPending} onClick={onClose}>Cancel</Button><Button type="submit" disabled={save.isPending || stations.isPending || slots.isFetching || !!referenceError}>{save.isPending ? 'Saving…' : 'Save'}</Button></div>
    </form>
  </Dialog>
}
