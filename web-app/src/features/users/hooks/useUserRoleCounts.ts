import { keepPreviousData, useQueries } from '@tanstack/react-query'
import * as usersApi from '@/features/users/api'
import { usersKeys } from '@/features/users/queryKeys'
import { userRoleTabs } from '@/features/users/roleTabs'
import type { UserRoleTabId } from '@/features/users/roleTabs'
import type { UserFilters } from '@/features/users/types'

export type UserRoleCounts = Partial<Record<UserRoleTabId, number>>

/** The active tab's own list query already carries this count, so it can skip its counts request. */
export interface ActiveTabCount {
  id: UserRoleTabId
  count: number
}

/**
 * Totals shown on the role tabs. The API has no counts endpoint, so each remaining tab asks for
 * a single-row page and keeps only `totalCount`. Counts honour the active search and status
 * filters so a tab badge always matches what selecting that tab would list.
 */
export function useUserRoleCounts(filters: UserFilters, activeTabCount?: ActiveTabCount): UserRoleCounts {
  const results = useQueries({
    queries: userRoleTabs.map((tab) => {
      const countFilters: UserFilters = { ...filters, role: tab.role, pageNumber: 1, pageSize: 1 }
      return {
        queryKey: usersKeys.list(countFilters),
        queryFn: () => usersApi.getUsers(countFilters),
        placeholderData: keepPreviousData,
        enabled: tab.id !== activeTabCount?.id,
      }
    }),
  })

  const counts: UserRoleCounts = {}
  userRoleTabs.forEach((tab, index) => {
    counts[tab.id] = tab.id === activeTabCount?.id ? activeTabCount.count : results[index].data?.totalCount
  })
  return counts
}
