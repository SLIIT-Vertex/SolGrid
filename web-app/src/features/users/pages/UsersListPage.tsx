import { useState } from 'react'
import { useAuth } from '@/auth/useAuth'
import { Button } from '@/components/common/Button'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'
import { EmptyState, ErrorState, LoadingState } from '@/components/common/QueryStates'
import { Pagination } from '@/components/common/Pagination'
import { useToast } from '@/components/common/useToast'
import { ResetPasswordDialog } from '@/features/users/components/ResetPasswordDialog'
import { UserCreateDialog } from '@/features/users/components/UserCreateDialog'
import { UserFilters } from '@/features/users/components/UserFilters'
import { UserRoleTabs } from '@/features/users/components/UserRoleTabs'
import { UserTable } from '@/features/users/components/UserTable'
import { useUsers } from '@/features/users/hooks/useUsers'
import { useUserRoleCounts } from '@/features/users/hooks/useUserRoleCounts'
import { useDeactivateUser, useReactivateUser } from '@/features/users/hooks/useSetUserActive'
import { findUserRoleTab, roleForTabId, tabIdForRole } from '@/features/users/roleTabs'
import type { UserRoleTabId } from '@/features/users/roleTabs'
import { defaultUserFilters } from '@/features/users/types'
import type { User } from '@/features/users/types'
import { getErrorMessage } from '@/lib/problemDetails'

export function UsersListPage() {
  const [filters, setFilters] = useState(defaultUserFilters)
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [pendingAction, setPendingAction] = useState<{ user: User; kind: 'deactivate' | 'reactivate' } | null>(
    null,
  )
  const [resetPasswordUser, setResetPasswordUser] = useState<User | null>(null)

  const { data, isLoading, isError, refetch } = useUsers(filters)
  const roleCounts = useUserRoleCounts(filters, data ? { id: tabIdForRole(filters.role), count: data.totalCount } : undefined)
  const deactivateUser = useDeactivateUser()
  const reactivateUser = useReactivateUser()
  const { showToast } = useToast()
  const { session } = useAuth()

  const isMutating = deactivateUser.isPending || reactivateUser.isPending
  const activeTabId = tabIdForRole(filters.role)
  const activeTab = findUserRoleTab(activeTabId)

  const handleTabChange = (tabId: UserRoleTabId) => {
    setFilters((current) => ({ ...current, role: roleForTabId(tabId), pageNumber: 1 }))
  }

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
    <div className="mx-auto max-w-6xl">
      <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-sm text-ink-500">
          Manage Backoffice and Grid Operator accounts that can sign in to SolGrid.
        </p>
        <Button type="button" onClick={() => setIsCreateOpen(true)} className="self-start sm:self-auto">
          New user
        </Button>
      </div>

      <div className="rounded-2xl border border-ink-100 bg-white">
        <div className="px-4 pt-2">
          <UserRoleTabs value={activeTabId} counts={roleCounts} onChange={handleTabChange} />
        </div>

        <div
          role="tabpanel"
          id="user-role-panel"
          aria-labelledby={`user-role-tab-${activeTabId}`}
          tabIndex={-1}
        >
          <div className="border-b border-ink-100 p-4">
            <UserFilters filters={filters} onChange={setFilters} />
          </div>

          {isLoading ? (
            <LoadingState label="Loading users…" />
          ) : isError ? (
            <ErrorState message="Couldn't load users." onRetry={() => refetch()} />
          ) : !data || data.items.length === 0 ? (
            <>
              <EmptyState title="No users found" description={activeTab.emptyDescription} />
              {data && data.totalCount > 0 ? (
                // The current page came back empty (e.g. the last user on it was just
                // deactivated) even though other pages still have matches — keep pagination
                // visible so "Previous" gets the admin back to real results.
                <Pagination
                  pageNumber={data.pageNumber}
                  pageSize={data.pageSize}
                  totalCount={data.totalCount}
                  onPageChange={(page) => setFilters((current) => ({ ...current, pageNumber: page }))}
                />
              ) : null}
            </>
          ) : (
            <>
              <UserTable
                users={data.items}
                currentUserId={session?.userId}
                onDeactivate={(user) => setPendingAction({ user, kind: 'deactivate' })}
                onReactivate={(user) => setPendingAction({ user, kind: 'reactivate' })}
                onResetPassword={setResetPasswordUser}
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

      <ResetPasswordDialog user={resetPasswordUser} onClose={() => setResetPasswordUser(null)} />
    </div>
  )
}
