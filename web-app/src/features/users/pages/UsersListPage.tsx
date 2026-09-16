import { useState } from 'react'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'
import { EmptyState, ErrorState, LoadingState } from '@/components/common/QueryStates'
import { Pagination } from '@/components/common/Pagination'
import { useToast } from '@/components/common/useToast'
import { UserCreateDialog } from '@/features/users/components/UserCreateDialog'
import { UserFilters } from '@/features/users/components/UserFilters'
import { UserTable } from '@/features/users/components/UserTable'
import { useUsers } from '@/features/users/hooks/useUsers'
import { useDeactivateUser, useReactivateUser } from '@/features/users/hooks/useSetUserActive'
import { defaultUserFilters } from '@/features/users/types'
import type { User } from '@/features/users/types'
import { getErrorMessage } from '@/lib/problemDetails'

export function UsersListPage() {
  const [filters, setFilters] = useState(defaultUserFilters)
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [pendingAction, setPendingAction] = useState<{ user: User; kind: 'deactivate' | 'reactivate' } | null>(
    null,
  )

  const { data, isLoading, isError, refetch } = useUsers(filters)
  const deactivateUser = useDeactivateUser()
  const reactivateUser = useReactivateUser()
  const { showToast } = useToast()

  const isMutating = deactivateUser.isPending || reactivateUser.isPending

  const handleConfirm = async () => {
    if (!pendingAction) return
    try {
      if (pendingAction.kind === 'deactivate') {
        await deactivateUser.mutateAsync(pendingAction.user.id)
        showToast(`${pendingAction.user.firstName} ${pendingAction.user.lastName} was deactivated.`)
      } else {
        await reactivateUser.mutateAsync(pendingAction.user.id)
        showToast(`${pendingAction.user.firstName} ${pendingAction.user.lastName} was reactivated.`)
      }
      setPendingAction(null)
    } catch (error) {
      showToast(getErrorMessage(error), 'error')
    }
  }

  return (
    <div className="mx-auto max-w-5xl">
      <div className="mb-6 flex items-center justify-between">
        <p className="text-sm text-ink-500">
          Manage Backoffice and Grid Operator accounts that can sign in to SolGrid.
        </p>
        <button
          type="button"
          onClick={() => setIsCreateOpen(true)}
          className="inline-flex h-10 items-center justify-center gap-2 rounded-lg bg-brand-600 px-4 text-sm font-medium text-white transition-colors hover:bg-brand-700"
        >
          New user
        </button>
      </div>

      <div className="rounded-2xl border border-ink-100 bg-white">
        <div className="border-b border-ink-100 p-4">
          <UserFilters filters={filters} onChange={setFilters} />
        </div>

        {isLoading ? (
          <LoadingState label="Loading users…" />
        ) : isError ? (
          <ErrorState message="Couldn't load users." onRetry={() => refetch()} />
        ) : !data || data.items.length === 0 ? (
          <EmptyState
            title="No users found"
            description="Try adjusting your filters, or create a new Backoffice or Grid Operator account."
          />
        ) : (
          <>
            <UserTable
              users={data.items}
              onDeactivate={(user) => setPendingAction({ user, kind: 'deactivate' })}
              onReactivate={(user) => setPendingAction({ user, kind: 'reactivate' })}
            />
            <Pagination
              pageNumber={data.pageNumber}
              pageSize={data.pageSize}
              totalCount={data.totalCount}
              onPageChange={(page) => setFilters((current) => ({ ...current, pageNumber: page }))}
            />
          </>
        )}
      </div>

      <ConfirmDialog
        open={pendingAction !== null}
        title={pendingAction?.kind === 'deactivate' ? 'Deactivate user' : 'Reactivate user'}
        description={
          pendingAction
            ? pendingAction.kind === 'deactivate'
              ? `${pendingAction.user.firstName} ${pendingAction.user.lastName} will no longer be able to sign in.`
              : `${pendingAction.user.firstName} ${pendingAction.user.lastName} will regain access to sign in.`
            : undefined
        }
        confirmLabel={pendingAction?.kind === 'deactivate' ? 'Deactivate' : 'Reactivate'}
        isDestructive={pendingAction?.kind === 'deactivate'}
        isLoading={isMutating}
        onConfirm={handleConfirm}
        onCancel={() => setPendingAction(null)}
      />

      <UserCreateDialog open={isCreateOpen} onClose={() => setIsCreateOpen(false)} />
    </div>
  )
}
