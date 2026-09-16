import { StatusBadge } from '@/components/common/StatusBadge'
import type { AccountStatus } from '@/auth/types'

export function AccountStatusBadge({ status }: { status: AccountStatus }) {
  return (
    <StatusBadge label={status} tone={status === 'Active' ? 'brand' : 'red'} />
  )
}
