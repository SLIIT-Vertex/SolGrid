import { keepPreviousData, useQueries } from '@tanstack/react-query'
import * as usersApi from '@/features/users/api'
import { usersKeys } from '@/features/users/queryKeys'
import { userRoleTabs } from '@/features/users/roleTabs'
import type { UserRoleTabId } from '@/features/users/roleTabs'
import type { UserFilters } from '@/features/users/types'

export type UserRoleCounts = Partial<Record<UserRoleTabId, number>>

/**
 * Totals shown on the role tabs. The API has no counts endpoint, so each tab asks for a
 * single-row page and keeps only `totalCount`. Counts honour the active search and status
 * filters so a tab badge always matches what selecting that tab would list.
 */
export function useUserRoleCounts(filters: UserFilters): UserRoleCounts {
  const results = useQueries({
    queries: userRoleTabs.map((tab) => {
      const countFilters: UserFilters = { ...filters, role: tab.role, pageNumber: 1, pageSize: 1 }
      return {
        queryKey: usersKeys.list(countFilters),
        queryFn: () => usersApi.getUsers(countFilters),
        placeholderData: keepPreviousData,
      }
    }),
  })

  const counts: UserRoleCounts = {}
  userRoleTabs.forEach((tab, index) => {
    counts[tab.id] = results[index].data?.totalCount
  })
  return counts
}
