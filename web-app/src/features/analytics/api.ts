import { apiClient } from '@/lib/apiClient'
import { toAnalyticsSnapshot } from '@/features/analytics/types'
import type { AnalyticsSnapshot, AnalyticsSnapshotDto } from '@/features/analytics/types'

export async function getAnalyticsSnapshot(): Promise<AnalyticsSnapshot> {
  const { data } = await apiClient.get<AnalyticsSnapshotDto>('/api/v1/analytics')
  return toAnalyticsSnapshot(data)
}
