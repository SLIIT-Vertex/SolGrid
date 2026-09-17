import { useMutation, useQueryClient } from '@tanstack/react-query'
import * as microgridApi from '@/features/microgrid/api'
import { microgridKeys } from '@/features/microgrid/queryKeys'
import type { UpdateSolarStationRequest } from '@/features/microgrid/types'

export function useUpdateMicrogridNode(id: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: UpdateSolarStationRequest) => microgridApi.updateStation(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: microgridKeys.lists() })
      queryClient.invalidateQueries({ queryKey: microgridKeys.detail(id) })
    },
  })
}
