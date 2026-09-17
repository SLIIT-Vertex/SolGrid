import { StatusBadge } from '@/components/common/StatusBadge'
import type { SlotStatus } from '@/features/microgrid/types'

const slotTones: Record<SlotStatus, 'brand' | 'amber' | 'ink' | 'red'> = {
  Available: 'brand',
  Reserved: 'amber',
  Occupied: 'ink',
  OutOfService: 'red',
}

const slotLabels: Record<SlotStatus, string> = {
  Available: 'Available',
  Reserved: 'Reserved',
  Occupied: 'Occupied',
  OutOfService: 'Out of service',
}

export function SlotStatusBadge({ status }: { status: SlotStatus }) {
  return <StatusBadge label={slotLabels[status]} tone={slotTones[status]} />
}
