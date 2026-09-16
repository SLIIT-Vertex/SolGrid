import { useQuery } from '@tanstack/react-query'
import * as usersApi from '@/features/users/api'
import { usersKeys } from '@/features/users/queryKeys'

export function useUser(id: string | undefined) {
  return useQuery({
    queryKey: usersKeys.detail(id ?? ''),
    queryFn: () => usersApi.getUser(id as string),
    enabled: Boolean(id),
  })
}
