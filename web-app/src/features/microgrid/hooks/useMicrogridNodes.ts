import { keepPreviousData, useQuery } from '@tanstack/react-query'
import * as microgridApi from '@/features/microgrid/api'
import { microgridKeys } from '@/features/microgrid/queryKeys'
import type { MicrogridNodeFilters } from '@/features/microgrid/types'

export function useMicrogridNodes(filters: MicrogridNodeFilters) {
  return useQuery({
    queryKey: microgridKeys.list(filters),
    queryFn: () => microgridApi.listStations(filters),
    placeholderData: keepPreviousData,
  })
}
