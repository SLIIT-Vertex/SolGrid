import { keepPreviousData, useQuery } from '@tanstack/react-query'
import * as microgridApi from '@/features/microgrid/api'
import { microgridKeys } from '@/features/microgrid/queryKeys'
import type { MicrogridSlotFilters } from '@/features/microgrid/types'
import { isNotFoundError } from '@/lib/problemDetails'

export function useMicrogridSlots(stationId: string | undefined, filters: MicrogridSlotFilters, enabled = true) {
  return useQuery({
    queryKey: microgridKeys.slotList(stationId ?? '', filters),
    queryFn: () => microgridApi.listSlots(stationId as string, filters),
    enabled: Boolean(stationId) && enabled,
    placeholderData: keepPreviousData,
    retry: (failureCount, error) => {
      if (isNotFoundError(error)) return false
      return failureCount < 1
    },
  })
}
