import { StatusBadge } from '@/components/common/StatusBadge'
import type { ReservationStatus } from '@/features/reservations/types'

const toneByStatus: Record<ReservationStatus, 'brand' | 'ink' | 'red' | 'amber'> = {
  Pending: 'amber',
  Approved: 'brand',
  Rejected: 'red',
  Cancelled: 'ink',
  Completed: 'brand',
}

export function ReservationStatusBadge({ status }: { status: ReservationStatus }) {
  return <StatusBadge label={status} tone={toneByStatus[status]} />
}
