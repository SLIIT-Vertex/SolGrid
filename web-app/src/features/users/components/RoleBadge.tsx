import { StatusBadge } from '@/components/common/StatusBadge'
import type { UserRole } from '@/auth/types'

export function RoleBadge({ role }: { role: UserRole }) {
  return (
    <StatusBadge
      label={role === 'Backoffice' ? 'Backoffice' : 'Grid Operator'}
      tone={role === 'Backoffice' ? 'brand' : 'ink'}
    />
  )
}
