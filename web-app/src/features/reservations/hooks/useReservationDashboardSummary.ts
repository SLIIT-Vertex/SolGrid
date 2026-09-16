import { useQuery } from '@tanstack/react-query'
import * as reservationsApi from '@/features/reservations/api'
import { reservationsKeys } from '@/features/reservations/queryKeys'

export function useReservationDashboardSummary() {
  return useQuery({
    queryKey: reservationsKeys.summary(),
    queryFn: () => reservationsApi.getDashboardSummary(),
  })
}
