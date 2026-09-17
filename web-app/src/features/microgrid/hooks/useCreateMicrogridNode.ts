import { useMutation, useQueryClient } from '@tanstack/react-query'
import * as microgridApi from '@/features/microgrid/api'
import { microgridKeys } from '@/features/microgrid/queryKeys'
import type { CreateSolarStationRequest } from '@/features/microgrid/types'

export function useCreateMicrogridNode() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateSolarStationRequest) => microgridApi.createStation(request),
    onSuccess: (node) => {
      queryClient.invalidateQueries({ queryKey: microgridKeys.lists() })
      queryClient.invalidateQueries({ queryKey: microgridKeys.detail(node.id) })
    },
  })
}
