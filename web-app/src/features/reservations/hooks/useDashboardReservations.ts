import { keepPreviousData, useQuery } from '@tanstack/react-query'
import * as reservationsApi from '@/features/reservations/api'
import { reservationsKeys } from '@/features/reservations/queryKeys'
import type { ReservationDashboardTab, ReservationFilters } from '@/features/reservations/types'

export function useDashboardReservations(tab: ReservationDashboardTab, filters: ReservationFilters) {
  return useQuery({
    queryKey: reservationsKeys.dashboard(tab, filters),
    queryFn: () => reservationsApi.getDashboardReservations(tab, filters),
    placeholderData: keepPreviousData,
  })
}
