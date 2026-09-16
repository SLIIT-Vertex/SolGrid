import { useMutation, useQueryClient } from '@tanstack/react-query'
import * as reservationsApi from '@/features/reservations/api'
import { reservationsKeys } from '@/features/reservations/queryKeys'
import type { RejectReservationInput } from '@/features/reservations/types'

function useInvalidateOnSettled() {
  const queryClient = useQueryClient()
  return (id: string) => {
    queryClient.invalidateQueries({ queryKey: reservationsKeys.lists() })
    queryClient.invalidateQueries({ queryKey: reservationsKeys.all })
    queryClient.invalidateQueries({ queryKey: reservationsKeys.detail(id) })
  }
}

export function useApproveReservation() {
  const invalidate = useInvalidateOnSettled()

  return useMutation({
    mutationFn: (id: string) => reservationsApi.approveReservation(id),
    onSuccess: (_, id) => invalidate(id),
  })
}

export function useRejectReservation() {
  const invalidate = useInvalidateOnSettled()

  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: RejectReservationInput }) =>
      reservationsApi.rejectReservation(id, input),
    onSuccess: (_, { id }) => invalidate(id),
  })
}

export function useCancelReservation() {
  const invalidate = useInvalidateOnSettled()

  return useMutation({
    mutationFn: (id: string) => reservationsApi.cancelReservation(id),
    onSuccess: (_, id) => invalidate(id),
  })
}
