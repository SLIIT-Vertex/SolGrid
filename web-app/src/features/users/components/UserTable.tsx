import { Link } from 'react-router-dom'
import { Button } from '@/components/common/Button'
import { RoleBadge } from '@/features/users/components/RoleBadge'
import { AccountStatusBadge } from '@/features/users/components/AccountStatusBadge'
import type { User } from '@/features/users/types'
import { cn } from '@/lib/cn'

interface UserTableProps {
  users: User[]
  onDeactivate: (user: User) => void
  onReactivate: (user: User) => void
}

const dateFormatter = new Intl.DateTimeFormat('en-GB', {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
})

export function UserTable({ users, onDeactivate, onReactivate }: UserTableProps) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[720px] text-left text-sm">
        <thead>
          <tr className="border-b border-ink-100 text-xs uppercase tracking-wide text-ink-400">
            <th className="px-4 py-3 font-medium">Name</th>
            <th className="px-4 py-3 font-medium">Email</th>
            <th className="px-4 py-3 font-medium">Role</th>
            <th className="px-4 py-3 font-medium">Status</th>
            <th className="px-4 py-3 font-medium">Created</th>
            <th className="px-4 py-3 font-medium text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100">
          {users.map((user) => (
            <tr key={user.id} className="hover:bg-ink-50/60">
              <td className="px-4 py-3">
                <Link
                  to={`/users/${user.id}/edit`}
                  className="font-medium text-ink-900 hover:text-brand-700"
                >
                  {user.firstName} {user.lastName}
                </Link>
              </td>
              <td className="px-4 py-3 text-ink-600">{user.email}</td>
              <td className="px-4 py-3">
                <RoleBadge role={user.role} />
              </td>
              <td className="px-4 py-3">
                <AccountStatusBadge status={user.status} />
              </td>
              <td className="px-4 py-3 text-ink-500">{dateFormatter.format(new Date(user.createdAt))}</td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-2">
                  <Link
                    to={`/users/${user.id}/edit`}
                    className={cn(
                      'inline-flex h-8 items-center justify-center rounded-lg border border-ink-200 bg-white px-3 text-sm font-medium text-ink-700 transition-colors hover:bg-ink-50',
                    )}
                  >
                    Edit
                  </Link>
                  {user.status === 'Active' ? (
                    <Button variant="danger" size="sm" onClick={() => onDeactivate(user)}>
                      Deactivate
                    </Button>
                  ) : (
                    <Button variant="secondary" size="sm" onClick={() => onReactivate(user)}>
                      Reactivate
                    </Button>
                  )}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
