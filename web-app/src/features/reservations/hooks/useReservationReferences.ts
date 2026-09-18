import { useQuery } from '@tanstack/react-query'
import { useAuth } from '@/auth/useAuth'
import { getProsumer } from '@/features/prosumers/api'
import { prosumersKeys } from '@/features/prosumers/queryKeys'
import { getSlot, getStation } from '@/features/microgrid/api'
import { microgridKeys } from '@/features/microgrid/queryKeys'
import type { Reservation } from '../types'
import { getReservationPermissions } from '../permissions'

export function useReservationReferences(reservation: Reservation) {
  const { session } = useAuth()
  const { canReadProsumers: canReadProsumer } = getReservationPermissions(
    session?.role,
  )
  const prosumer = useQuery({
    queryKey: prosumersKeys.detail(reservation.prosumerId),
    queryFn: () => getProsumer(reservation.prosumerId),
    enabled: canReadProsumer && !reservation.prosumerDetails,
    staleTime: 60_000,
    retry: false,
  })
  const station = useQuery({
    queryKey: microgridKeys.detail(reservation.stationId),
    queryFn: () => getStation(reservation.stationId),
    staleTime: 60_000,
    retry: false,
  })
  const slot = useQuery({
    queryKey: [...microgridKeys.all, 'slot-detail', reservation.bookingSlotId],
    queryFn: () => getSlot(reservation.bookingSlotId),
    staleTime: 60_000,
    retry: false,
  })
  return {
    prosumer,
    person:
      reservation.prosumerDetails ?? (canReadProsumer ? prosumer.data : undefined),
    station,
    slot,
    canReadProsumer,
  }
}
