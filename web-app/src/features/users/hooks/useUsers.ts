import { keepPreviousData, useQuery } from '@tanstack/react-query'
import * as usersApi from '@/features/users/api'
import { usersKeys } from '@/features/users/queryKeys'
import type { UserFilters } from '@/features/users/types'

export function useUsers(filters: UserFilters) {
  return useQuery({
    queryKey: usersKeys.list(filters),
    queryFn: () => usersApi.getUsers(filters),
    placeholderData: keepPreviousData,
  })
}
