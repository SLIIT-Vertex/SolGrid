import { Link } from 'react-router-dom'
import { Button } from '@/components/common/Button'
import { RoleBadge } from '@/features/users/components/RoleBadge'
import { AccountStatusBadge } from '@/features/users/components/AccountStatusBadge'
import { isCurrentUser as checkIsCurrentUser } from '@/features/users/permissions'
import type { User } from '@/features/users/types'
import { cn } from '@/lib/cn'

interface UserTableProps {
  users: User[]
  currentUserId?: string
  onDeactivate: (user: User) => void
  onReactivate: (user: User) => void
  onResetPassword: (user: User) => void
}

const dateFormatter = new Intl.DateTimeFormat('en-GB', {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
})

function getFullName(user: User): string {
  return `${user.firstName} ${user.lastName}`
}

function UserActions({
  user,
  isCurrentUser,
  onDeactivate,
  onReactivate,
  onResetPassword,
}: {
  user: User
  isCurrentUser: boolean
  onDeactivate: (user: User) => void
  onReactivate: (user: User) => void
  onResetPassword: (user: User) => void
}) {
  const fullName = getFullName(user)

  return (
    <div className="flex flex-wrap items-center justify-end gap-2">
      <Link
        to={`/users/${user.id}/edit`}
        aria-label={`Edit ${fullName}`}
        className={cn(
          'inline-flex h-8 items-center justify-center rounded-lg border border-ink-200 bg-white px-3 text-sm font-medium text-ink-700 transition-colors hover:bg-ink-50',
          'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ink-400',
        )}
      >
        Edit
      </Link>
      <Button
        variant="secondary"
        size="sm"
        aria-label={`Reset password for ${fullName}`}
        onClick={() => onResetPassword(user)}
      >
        Reset password
      </Button>
      {isCurrentUser ? (
        <span className="px-2 text-xs font-medium text-ink-400" title="You cannot deactivate your own account">
          Current account
        </span>
      ) : user.status === 'Active' ? (
        <Button
          variant="danger"
          size="sm"
          aria-label={`Deactivate ${fullName}`}
          onClick={() => onDeactivate(user)}
        >
          Deactivate
        </Button>
      ) : (
        <Button
          variant="secondary"
          size="sm"
          aria-label={`Reactivate ${fullName}`}
          onClick={() => onReactivate(user)}
        >
          Reactivate
        </Button>
      )}
    </div>
  )
}

export function UserTable({ users, currentUserId, onDeactivate, onReactivate, onResetPassword }: UserTableProps) {
  return (
    <>
      <div className="divide-y divide-ink-100 lg:hidden">
        {users.map((user) => {
          const isCurrentUser = checkIsCurrentUser(currentUserId, user)
          return (
            <article key={user.id} className="space-y-4 p-4">
              <div className="min-w-0">
                <Link
                  to={`/users/${user.id}/edit`}
                  className="font-medium text-ink-900 hover:text-brand-700"
                >
                  {getFullName(user)}
                </Link>
                {isCurrentUser ? (
                  <span className="ml-2 text-xs font-medium text-brand-700">You</span>
                ) : null}
                <p className="mt-1 break-all text-sm text-ink-600">{user.email}</p>
              </div>
              <dl className="grid grid-cols-2 gap-3 text-sm">
                <div>
                  <dt className="mb-1 text-xs uppercase tracking-wide text-ink-400">Role</dt>
                  <dd>
                    <RoleBadge role={user.role} />
                  </dd>
                </div>
                <div>
                  <dt className="mb-1 text-xs uppercase tracking-wide text-ink-400">Status</dt>
                  <dd>
                    <AccountStatusBadge status={user.status} />
                  </dd>
                </div>
                <div className="col-span-2">
                  <dt className="text-xs uppercase tracking-wide text-ink-400">Created</dt>
                  <dd className="mt-1 text-ink-600">{dateFormatter.format(new Date(user.createdAt))}</dd>
                </div>
              </dl>
              <UserActions
                user={user}
                isCurrentUser={isCurrentUser}
                onDeactivate={onDeactivate}
                onReactivate={onReactivate}
                onResetPassword={onResetPassword}
              />
            </article>
          )
        })}
      </div>

      <div className="hidden overflow-x-auto lg:block">
        <table className="w-full min-w-[800px] text-left text-sm">
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
            {users.map((user) => {
              const isCurrentUser = checkIsCurrentUser(currentUserId, user)
              return (
                <tr key={user.id} className="hover:bg-ink-50/60">
                  <td className="px-4 py-3">
                    <Link
                      to={`/users/${user.id}/edit`}
                      className="font-medium text-ink-900 hover:text-brand-700"
                    >
                      {getFullName(user)}
                    </Link>
                    {isCurrentUser ? (
                      <span className="ml-2 text-xs font-medium text-brand-700">You</span>
                    ) : null}
                  </td>
                  <td className="px-4 py-3 text-ink-600">{user.email}</td>
                  <td className="px-4 py-3">
                    <RoleBadge role={user.role} />
                  </td>
                  <td className="px-4 py-3">
                    <AccountStatusBadge status={user.status} />
                  </td>
                  <td className="px-4 py-3 text-ink-500">
                    {dateFormatter.format(new Date(user.createdAt))}
                  </td>
                  <td className="px-4 py-3">
                    <UserActions
                      user={user}
                      isCurrentUser={isCurrentUser}
                      onDeactivate={onDeactivate}
                      onReactivate={onReactivate}
                      onResetPassword={onResetPassword}
                    />
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </>
  )
}
