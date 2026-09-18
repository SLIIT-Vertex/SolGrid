import { StatusBadge } from '@/components/common/StatusBadge'
import { roleLabel, type UserRole } from '@/auth/types'

export function RoleBadge({ role }: { role: UserRole }) {
  return (
    <StatusBadge
      label={roleLabel(role)}
      tone={role === 'Backoffice' ? 'brand' : 'ink'}
    />
  )
}
