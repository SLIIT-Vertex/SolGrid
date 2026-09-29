import { StatusBadge } from '@/components/common/StatusBadge'
import type { ProsumerAccountStatus } from '@/features/prosumers/types'

const toneByStatus: Record<ProsumerAccountStatus, 'brand' | 'ink' | 'red' | 'amber'> = {
  Pending: 'amber',
  Active: 'brand',
  DeactivationRequested: 'amber',
  Deactivated: 'red',
}

const labelByStatus: Record<ProsumerAccountStatus, string> = {
  Pending: 'Pending',
  Active: 'Active',
  DeactivationRequested: 'Deactivation requested',
  Deactivated: 'Deactivated',
}

export function prosumerStatusLabel(status: ProsumerAccountStatus): string {
  return labelByStatus[status]
}

export function ProsumerStatusBadge({ status }: { status: ProsumerAccountStatus }) {
  return <StatusBadge label={prosumerStatusLabel(status)} tone={toneByStatus[status]} />
}
