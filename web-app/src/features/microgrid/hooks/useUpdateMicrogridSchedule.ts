import { useMutation, useQueryClient } from '@tanstack/react-query'
import * as microgridApi from '@/features/microgrid/api'
import { microgridKeys } from '@/features/microgrid/queryKeys'
import type { UpdateStationScheduleRequest } from '@/features/microgrid/types'

export function useUpdateMicrogridSchedule(id: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: UpdateStationScheduleRequest) => microgridApi.updateSchedule(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: microgridKeys.lists() })
      queryClient.invalidateQueries({ queryKey: microgridKeys.detail(id) })
    },
  })
}
