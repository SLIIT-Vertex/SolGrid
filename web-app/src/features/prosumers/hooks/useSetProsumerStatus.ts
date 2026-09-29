import { useMutation, useQueryClient } from '@tanstack/react-query'
import * as prosumersApi from '@/features/prosumers/api'
import type { ProsumerLifecycleChange } from '@/features/prosumers/types'
import { prosumersKeys } from '@/features/prosumers/queryKeys'

function useProsumerStatusMutation(mutationFn: (request: ProsumerLifecycleChange) => Promise<void>) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn,
    onSettled: () => {
      // a rejected conflict still leaves the list stale
      queryClient.invalidateQueries({ queryKey: prosumersKeys.all })
    },
  })
}

export function useActivateProsumer() {
  return useProsumerStatusMutation(prosumersApi.activateProsumer)
}

export function useDeactivateProsumer() {
  return useProsumerStatusMutation(prosumersApi.deactivateProsumer)
}

export function useReactivateProsumer() {
  return useProsumerStatusMutation(prosumersApi.reactivateProsumer)
}
