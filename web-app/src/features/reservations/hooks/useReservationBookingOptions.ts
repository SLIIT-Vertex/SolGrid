import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useAuth } from '@/auth/useAuth'
import { getProsumer } from '@/features/prosumers/api'
import { prosumersKeys } from '@/features/prosumers/queryKeys'
import { getStation, listStations, listSlots } from '@/features/microgrid/api'
import {
  defaultMicrogridNodeFilters,
  defaultMicrogridSlotFilters,
} from '@/features/microgrid/types'
import { microgridKeys } from '@/features/microgrid/queryKeys'
import { allPages } from '@/lib/pagination'
import type { Reservation } from '../types'
import { getReservationPermissions } from '../permissions'

export function useReservationBookingOptions(
  reservation: Reservation | undefined,
  stationId: string,
  bookingSlotId: string,
) {
  const { session } = useAuth()
  const { canReadProsumers } = getReservationPermissions(session?.role)
  const profile = useQuery({
    queryKey: prosumersKeys.detail(reservation?.prosumerId ?? ''),
    queryFn: () => getProsumer(reservation?.prosumerId ?? ''),
    enabled: !!reservation && canReadProsumers,
    retry: false,
  })
  const stations = useQuery({
    queryKey: [...microgridKeys.all, 'reservation-stations'],
    queryFn: () =>
      allPages((pageNumber) =>
        listStations({
          ...defaultMicrogridNodeFilters,
          status: 'Active',
          pageNumber,
          pageSize: 100,
        }),
      ),
  })
  const currentStation = useQuery({
    queryKey: microgridKeys.detail(reservation?.stationId ?? ''),
    queryFn: () => getStation(reservation?.stationId ?? ''),
    enabled: !!reservation,
  })
  const selectedStation =
    stations.data?.find((node) => node.id === stationId) ??
    (currentStation.data?.id === stationId ? currentStation.data : undefined)
  const slots = useQuery({
    queryKey: [...microgridKeys.slots(stationId), 'reservation-options'],
    enabled: !!stationId,
    queryFn: () =>
      allPages((pageNumber) =>
        listSlots(stationId, {
          ...defaultMicrogridSlotFilters,
          pageNumber,
          pageSize: 100,
        }),
      ),
  })
  const sortedSlots = useMemo(
    () =>
      [...(slots.data ?? [])].sort(
        (a, b) =>
          new Date(a.startTime).getTime() - new Date(b.startTime).getTime() ||
          a.slotNumber - b.slotNumber,
      ),
    [slots.data],
  )
  const selectedSlot = sortedSlots.find((slot) => slot.id === bookingSlotId)
  return {
    person: canReadProsumers ? profile.data : null,
    stations,
    selectedStation,
    slots,
    sortedSlots,
    selectedSlot,
  }
}
