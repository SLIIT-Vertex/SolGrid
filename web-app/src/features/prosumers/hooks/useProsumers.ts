import { keepPreviousData, useQuery } from '@tanstack/react-query'
import * as prosumersApi from '@/features/prosumers/api'
import { prosumersKeys } from '@/features/prosumers/queryKeys'
import type { ProsumerFilters } from '@/features/prosumers/types'

export function useProsumers(filters: ProsumerFilters) {
  return useQuery({
    queryKey: prosumersKeys.list(filters),
    queryFn: () => prosumersApi.getProsumers(filters),
    placeholderData: keepPreviousData,
  })
}
