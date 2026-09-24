import { useQuery } from '@tanstack/react-query'
import * as analyticsApi from '@/features/analytics/api'
import { analyticsKeys } from '@/features/analytics/queryKeys'

export function useAnalytics() {
  return useQuery({
    queryKey: analyticsKeys.snapshot(),
    queryFn: () => analyticsApi.getAnalyticsSnapshot(),
  })
}
