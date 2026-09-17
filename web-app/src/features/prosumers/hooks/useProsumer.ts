import { useQuery } from '@tanstack/react-query'
import * as prosumersApi from '@/features/prosumers/api'
import { prosumersKeys } from '@/features/prosumers/queryKeys'

export function useProsumer(nic: string | undefined) {
  return useQuery({
    queryKey: prosumersKeys.detail(nic ?? ''),
    queryFn: () => prosumersApi.getProsumer(nic ?? ''),
    enabled: Boolean(nic),
  })
}
