import { useMutation, useQueryClient } from '@tanstack/react-query'
import * as prosumersApi from '@/features/prosumers/api'
import { prosumersKeys } from '@/features/prosumers/queryKeys'

function useProsumerStatusMutation(mutationFn: (nic: string) => Promise<void>) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn,
    onSuccess: (_, nic) => {
      queryClient.invalidateQueries({ queryKey: prosumersKeys.lists() })
      queryClient.invalidateQueries({ queryKey: prosumersKeys.detail(nic) })
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
