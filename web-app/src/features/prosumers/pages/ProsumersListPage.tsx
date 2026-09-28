import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Button } from '@/components/common/Button'
import { ProsumerDialog } from '@/features/prosumers/components/ProsumerDialog'
import { Dialog } from '@/components/common/Dialog'
import { ProsumerHistoryDialog } from '@/features/prosumers/components/ProsumerHistoryDialog'
import { isConflictError } from '@/lib/problemDetails'
import { EmptyState, ErrorState, LoadingState } from '@/components/common/QueryStates'
import { Pagination } from '@/components/common/Pagination'
import { useToast } from '@/components/common/useToast'
import { ProsumerFilters } from '@/features/prosumers/components/ProsumerFilters'
import { ProsumerSummaryCards } from '@/features/prosumers/components/ProsumerSummaryCards'
import { ProsumerTable } from '@/features/prosumers/components/ProsumerTable'
import type { ProsumerAction } from '@/features/prosumers/components/ProsumerTable'
import { useProsumerStatusCounts } from '@/features/prosumers/hooks/useProsumerStatusCounts'
import { useProsumers } from '@/features/prosumers/hooks/useProsumers'
import {
  useActivateProsumer,
  useDeactivateProsumer,
  useReactivateProsumer,
} from '@/features/prosumers/hooks/useSetProsumerStatus'
import { defaultProsumerFilters } from '@/features/prosumers/types'
import type { Prosumer, ProsumerAccountStatus } from '@/features/prosumers/types'
import { getErrorMessage } from '@/lib/problemDetails'

const actionCopy: Record<
  ProsumerAction,
  { title: string; confirmLabel: string; verb: string; effect: string; needsReason: boolean }
> = {
  activate: {
    title: 'Activate account',
    confirmLabel: 'Activate',
    verb: 'activated',
    effect: 'They will be able to sign in to the mobile app and book battery slots.',
    needsReason: false,
  },
  reject: {
    title: 'Reject registration',
    confirmLabel: 'Reject',
    verb: 'rejected',
    effect: 'The registration will not be approved and they will not be able to sign in. You can reactivate the account later if needed.',
    needsReason: true,
  },
  deactivate: {
    title: 'Deactivate account',
    confirmLabel: 'Deactivate',
    verb: 'deactivated',
    effect: 'They will lose access to the mobile app. Existing bookings stay on record and are not cancelled.',
    needsReason: true,
  },
  reactivate: {
    title: 'Reactivate account',
    confirmLabel: 'Reactivate',
    verb: 'reactivated',
    effect: 'They will regain access to the mobile app.',
    needsReason: true,
  },
}

function statusFromQuery(value: string | null): ProsumerAccountStatus | '' {
  return value === 'Pending' || value === 'Active' || value === 'DeactivationRequested' || value === 'Deactivated'
    ? value
    : ''
}

export function ProsumersListPage() {
  const [searchParams] = useSearchParams()
  const [history, setHistory] = useState<Prosumer | null>(null)
  const [reason, setReason] = useState('')
  const [actionError, setActionError] = useState<string | null>(null)
  const [actionConflicted, setActionConflicted] = useState(false)
  const [editor, setEditor] = useState<{ prosumer?: Prosumer } | null>(null)
  const [filterResetKey, setFilterResetKey] = useState(0)
  const [filters, setFilters] = useState(() => ({
    ...defaultProsumerFilters,
    status: statusFromQuery(searchParams.get('status')),
  }))
  const [pendingAction, setPendingAction] = useState<{ prosumer: Prosumer; action: ProsumerAction } | null>(
    null,
  )

  const { data, isLoading, isError, refetch } = useProsumers(filters)
  const { data: counts, isLoading: isCountsLoading } = useProsumerStatusCounts()
  const activateProsumer = useActivateProsumer()
  const deactivateProsumer = useDeactivateProsumer()
  const reactivateProsumer = useReactivateProsumer()
  const { showToast } = useToast()

  const isMutating =
    activateProsumer.isPending || deactivateProsumer.isPending || reactivateProsumer.isPending
  const hasActiveFilters = Boolean(filters.searchText.trim() || filters.status)
  const pendingCopy = pendingAction ? actionCopy[pendingAction.action] : null
  const reasonMissing = Boolean(pendingCopy?.needsReason && !reason.trim())

  const clearFilters = () => {
    setFilters(defaultProsumerFilters)
    setFilterResetKey((current) => current + 1)
  }

  const mutationByAction: Record<ProsumerAction, typeof activateProsumer> = {
    activate: activateProsumer,
    reject: deactivateProsumer,
    deactivate: deactivateProsumer,
    reactivate: reactivateProsumer,
  }

  const handleConfirm = async () => {
    if (!pendingAction || reasonMissing || actionConflicted) return
    const { prosumer, action } = pendingAction
    try {
      await mutationByAction[action].mutateAsync({ nic: prosumer.nic, expectedVersion: prosumer.version, reason: reason.trim() })
      showToast(`${prosumer.firstName} ${prosumer.lastName} was ${actionCopy[action].verb}.`)
      setPendingAction(null)
    } catch (error) {
      setActionError(getErrorMessage(error))
      setActionConflicted(isConflictError(error))
    }
  }

  return (
    <div className="mx-auto max-w-7xl">
      <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <p className="max-w-2xl text-sm text-ink-500">
          Review account requests, update contact details, and manage prosumer access.
        </p>
        <Button type="button" className="w-full sm:w-auto" onClick={() => setEditor({})}>
          Add prosumer
        </Button>
      </div>
      {history && <ProsumerHistoryDialog prosumer={history} onClose={() => setHistory(null)} />}
      {editor && <ProsumerDialog prosumer={editor.prosumer} onClose={() => setEditor(null)} />}
      <ProsumerSummaryCards
        counts={counts}
        isLoading={isCountsLoading}
        selectedStatus={filters.status}
        onSelect={(status) => setFilters((current) => ({ ...current, status, pageNumber: 1 }))}
      />
      {filters.status === 'DeactivationRequested' && (
        <p className="mt-4 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
          Review each request in History. Accounts remain usable until Backoffice confirms deactivation. Existing bookings are not automatically cancelled.
        </p>
      )}
      <div className="mt-6 rounded-2xl border border-ink-100 bg-white">
        <div className="border-b border-ink-100 p-4">
          <ProsumerFilters
            key={filterResetKey}
            filters={filters}
            onChange={setFilters}
            onClear={clearFilters}
            hasActiveFilters={hasActiveFilters}
          />
        </div>

        {isLoading ? (
          <LoadingState label="Loading prosumers…" />
        ) : isError ? (
          <ErrorState message="Couldn't load prosumers." onRetry={() => refetch()} />
        ) : !data || data.items.length === 0 ? (
          <EmptyState
            title="No prosumers found"
            description={hasActiveFilters
              ? 'No accounts match the current filters.'
              : 'Add a prosumer profile to get started.'}
            action={hasActiveFilters ? (
              <Button type="button" variant="secondary" onClick={clearFilters}>
                Clear filters
              </Button>
            ) : undefined}
          />
        ) : (
          <>
            <ProsumerTable
              prosumers={data.items}
              onEdit={(prosumer) => setEditor({ prosumer })}
              onHistory={setHistory}
              onAction={(prosumer, action) => {
                setReason('')
                setActionError(null)
                setActionConflicted(false)
                setPendingAction({ prosumer, action })
              }}
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

      <Dialog
        open={pendingAction !== null}
        title={pendingCopy?.title ?? ''}
        onClose={() => { if (!isMutating) setPendingAction(null) }}
        description={pendingAction && pendingCopy
          ? `${pendingAction.prosumer.firstName} ${pendingAction.prosumer.lastName} · NIC ${pendingAction.prosumer.nic}`
          : undefined}
      >
        <form
          onSubmit={(event) => {
            event.preventDefault()
            void handleConfirm()
          }}
        >
          <p className="mb-3 text-sm text-ink-600">{pendingCopy?.effect}</p>
          {pendingCopy?.needsReason ? (
            <>
              <label htmlFor="account-action-reason" className="text-sm font-medium text-ink-700">
                Reason
              </label>
              <textarea
                id="account-action-reason"
                required
                maxLength={500}
                value={reason}
                onChange={(event) => setReason(event.target.value)}
                disabled={isMutating || actionConflicted}
                className="mt-2 w-full rounded-lg border border-ink-200 p-3 text-sm"
                rows={3}
              />
              <p className="mt-1 text-xs text-ink-500">Recorded in account history and visible to the prosumer.</p>
            </>
          ) : null}
          {actionError ? <p role="alert" className="mt-3 text-sm text-red-600">{actionError}</p> : null}
          <div className="mt-5 flex justify-end gap-2">
            <Button type="button" variant="secondary" disabled={isMutating} onClick={() => setPendingAction(null)}>
              {actionConflicted ? 'Close and review latest account' : 'Cancel'}
            </Button>
            <Button
              type="submit"
              variant={pendingAction?.action === 'deactivate' || pendingAction?.action === 'reject' ? 'danger' : 'primary'}
              isLoading={isMutating}
              disabled={reasonMissing || actionConflicted}
            >
              {pendingCopy?.confirmLabel}
            </Button>
          </div>
        </form>
      </Dialog>
    </div>
  )
}
