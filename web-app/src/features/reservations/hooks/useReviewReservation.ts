import { useMutation, useQueryClient } from '@tanstack/react-query'
import * as reservationsApi from '@/features/reservations/api'
import { reservationsKeys } from '@/features/reservations/queryKeys'
import type { RejectReservationInput } from '@/features/reservations/types'
import { microgridKeys } from '@/features/microgrid/queryKeys'

function useInvalidateReservationQueries() {
  const queryClient = useQueryClient()
  return () => {
    void queryClient.invalidateQueries({ queryKey: reservationsKeys.all })
    void queryClient.invalidateQueries({ queryKey: microgridKeys.all })
  }
}

export function useApproveReservation() {
  const invalidate = useInvalidateReservationQueries()

  return useMutation({
    mutationFn: (id: string) => reservationsApi.approveReservation(id),
    onSuccess: () => invalidate(),
  })
}

export function useRejectReservation() {
  const invalidate = useInvalidateReservationQueries()

  return useMutation({
    mutationFn: ({
      id,
      input,
    }: {
      id: string
      input: RejectReservationInput
    }) => reservationsApi.rejectReservation(id, input),
    onSuccess: () => invalidate(),
  })
}

export function useCancelReservation() {
  const invalidate = useInvalidateReservationQueries()

  return useMutation({
    mutationFn: (id: string) => reservationsApi.cancelReservation(id),
    onSuccess: () => invalidate(),
  })
}
