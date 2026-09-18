import { useQuery } from '@tanstack/react-query'
import * as prosumersApi from '@/features/prosumers/api'
import { prosumersKeys } from '@/features/prosumers/queryKeys'

export function useProsumerStatusCounts() {
  return useQuery({
    queryKey: prosumersKeys.summary(),
    queryFn: () => prosumersApi.getProsumerStatusCounts(),
  })
}
