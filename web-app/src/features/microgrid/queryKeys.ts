import type { MicrogridNodeFilters, MicrogridSlotFilters } from '@/features/microgrid/types'

export const microgridKeys = {
  all: ['microgrid'] as const,
  lists: () => [...microgridKeys.all, 'list'] as const,
  list: (filters: MicrogridNodeFilters) => [...microgridKeys.lists(), filters] as const,
  details: () => [...microgridKeys.all, 'detail'] as const,
  detail: (id: string) => [...microgridKeys.details(), id] as const,
  slots: (stationId: string) => [...microgridKeys.all, 'slots', stationId] as const,
  slotList: (stationId: string, filters: MicrogridSlotFilters) =>
    [...microgridKeys.slots(stationId), filters] as const,
}
