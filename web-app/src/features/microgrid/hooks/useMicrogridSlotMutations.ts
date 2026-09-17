import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { QueryClient } from '@tanstack/react-query'
import * as microgridApi from '@/features/microgrid/api'
import { microgridKeys } from '@/features/microgrid/queryKeys'
import type { CreateBookingSlotRequest, UpdateBookingSlotRequest } from '@/features/microgrid/types'

function invalidateSlotQueries(queryClient: QueryClient, stationId: string) {
  queryClient.invalidateQueries({ queryKey: microgridKeys.slots(stationId) })
  queryClient.invalidateQueries({ queryKey: microgridKeys.detail(stationId) })
  queryClient.invalidateQueries({ queryKey: microgridKeys.lists() })
}

export function useCreateMicrogridSlot(stationId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateBookingSlotRequest) => microgridApi.createSlot(stationId, request),
    onSuccess: () => invalidateSlotQueries(queryClient, stationId),
  })
}

export function useUpdateMicrogridSlot(stationId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateBookingSlotRequest }) =>
      microgridApi.updateSlot(id, request),
    onSuccess: () => invalidateSlotQueries(queryClient, stationId),
  })
}

export function useActivateMicrogridSlot(stationId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => microgridApi.activateSlot(id),
    onSuccess: () => invalidateSlotQueries(queryClient, stationId),
  })
}

export function useDeactivateMicrogridSlot(stationId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => microgridApi.deactivateSlot(id),
    onSuccess: () => invalidateSlotQueries(queryClient, stationId),
  })
}
