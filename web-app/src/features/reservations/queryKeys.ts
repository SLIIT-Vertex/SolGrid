import type { ReservationDashboardTab, ReservationFilters } from '@/features/reservations/types'

export const reservationsKeys = {
  all: ['reservations'] as const,
  lists: () => [...reservationsKeys.all, 'list'] as const,
  list: (filters: ReservationFilters) => [...reservationsKeys.lists(), filters] as const,
  dashboard: (tab: ReservationDashboardTab, filters: ReservationFilters) =>
    [...reservationsKeys.all, 'dashboard', tab, filters] as const,
  summary: () => [...reservationsKeys.all, 'summary'] as const,
  details: () => [...reservationsKeys.all, 'detail'] as const,
  detail: (id: string) => [...reservationsKeys.details(), id] as const,
}
