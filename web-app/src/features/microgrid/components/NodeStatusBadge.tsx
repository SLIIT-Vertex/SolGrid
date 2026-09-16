import { StatusBadge } from '@/components/common/StatusBadge'
import type { StationStatus } from '@/features/microgrid/types'

export function NodeStatusBadge({ status }: { status: StationStatus }) {
  return <StatusBadge label={status} tone={status === 'Active' ? 'brand' : 'red'} />
}
