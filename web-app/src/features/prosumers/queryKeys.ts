import type { ProsumerFilters } from '@/features/prosumers/types'

export const prosumersKeys = {
  all: ['prosumers'] as const,
  lists: () => [...prosumersKeys.all, 'list'] as const,
  list: (filters: ProsumerFilters) => [...prosumersKeys.lists(), filters] as const,
  details: () => [...prosumersKeys.all, 'detail'] as const,
  detail: (nic: string) => [...prosumersKeys.details(), nic] as const,
}
