import { useQuery } from '@tanstack/react-query'
import * as microgridApi from '@/features/microgrid/api'
import { microgridKeys } from '@/features/microgrid/queryKeys'
import { isNotFoundError } from '@/lib/problemDetails'

export function useMicrogridNode(id: string | undefined) {
  return useQuery({
    queryKey: microgridKeys.detail(id ?? ''),
    queryFn: () => microgridApi.getStation(id as string),
    enabled: Boolean(id),
    retry: (failureCount, error) => {
      if (isNotFoundError(error)) return false
      return failureCount < 1
    },
  })
}
