import { keepPreviousData, useQuery } from '@tanstack/react-query'
import * as reservationsApi from '@/features/reservations/api'
import { reservationsKeys } from '@/features/reservations/queryKeys'
import type { ReservationFilters } from '@/features/reservations/types'

export function useReservations(filters: ReservationFilters) {
  return useQuery({
    queryKey: reservationsKeys.list(filters),
    queryFn: () => reservationsApi.getReservations(filters),
    placeholderData: keepPreviousData,
  })
}
