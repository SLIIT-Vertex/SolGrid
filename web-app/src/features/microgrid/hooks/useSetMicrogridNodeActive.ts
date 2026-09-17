import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { QueryClient } from '@tanstack/react-query'
import * as microgridApi from '@/features/microgrid/api'
import { microgridKeys } from '@/features/microgrid/queryKeys'

function invalidateNodeLifecycleQueries(queryClient: QueryClient, id: string) {
  queryClient.invalidateQueries({ queryKey: microgridKeys.lists() })
  queryClient.invalidateQueries({ queryKey: microgridKeys.detail(id) })
  queryClient.invalidateQueries({ queryKey: microgridKeys.slots(id) })
}

export function useActivateMicrogridNode(id: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => microgridApi.activateStation(id),
    onSuccess: () => invalidateNodeLifecycleQueries(queryClient, id),
  })
}

export function useDeactivateMicrogridNode(id: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => microgridApi.deactivateStation(id),
    onSuccess: () => invalidateNodeLifecycleQueries(queryClient, id),
  })
}
