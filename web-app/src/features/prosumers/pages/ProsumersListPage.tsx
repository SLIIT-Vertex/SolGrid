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
  { title: string; verb: string; isDestructive?: boolean }
> = {
  activate: { title: 'Activate prosumer', verb: 'activated' },
  deactivate: { title: 'Deactivate prosumer', verb: 'deactivated', isDestructive: true },
  reactivate: { title: 'Reactivate prosumer', verb: 'reactivated' },
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

  const mutationByAction: Record<ProsumerAction, typeof activateProsumer> = {
    activate: activateProsumer,
    deactivate: deactivateProsumer,
    reactivate: reactivateProsumer,
  }

  const handleConfirm = async () => {
    if (!pendingAction || !reason.trim() || actionConflicted) return
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
    <div className="mx-auto max-w-6xl">
      <div className="mb-4 flex justify-end">
        <Button type="button" onClick={() => setEditor({})}>
          Create prosumer
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
        <p className="mt-4 text-sm text-ink-600">Review each request in History. Accounts remain usable until Backoffice confirms deactivation. Existing bookings are not automatically cancelled.</p>
      )}
      <div className="mt-6 rounded-2xl border border-ink-100 bg-white">
        <div className="border-b border-ink-100 p-4">
          <ProsumerFilters filters={filters} onChange={setFilters} />
        </div>

        {isLoading ? (
          <LoadingState label="Loading prosumers…" />
        ) : isError ? (
          <ErrorState message="Couldn't load prosumers." onRetry={() => refetch()} />
        ) : !data || data.items.length === 0 ? (
          <EmptyState
            title="No prosumers found"
            description="Try adjusting your filters, or create a profile if needed."
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
        title={pendingAction ? actionCopy[pendingAction.action].title : ''}
        onClose={() => { if (!isMutating) setPendingAction(null) }}
        description={pendingAction
          ? `${pendingAction.prosumer.firstName} ${pendingAction.prosumer.lastName} (${pendingAction.prosumer.nic}) will be ${actionCopy[pendingAction.action].verb}.`
          : undefined}
      >
        <form onSubmit={(event) => { event.preventDefault(); void handleConfirm() }}>
          {pendingAction?.action === 'deactivate' && <p className="mb-3 text-sm text-ink-600">This ends account access. Existing bookings remain on record and are not automatically cancelled.</p>}
          <label htmlFor="account-action-reason" className="text-sm font-medium text-ink-700">Reason</label>
          <textarea id="account-action-reason" required maxLength={500} value={reason}
            onChange={(event) => setReason(event.target.value)} disabled={isMutating || actionConflicted}
            className="mt-2 w-full rounded-lg border border-ink-200 p-3 text-sm" rows={3} />
          <p className="mt-1 text-xs text-ink-500">Recorded in account history and visible to the prosumer when they have account access.</p>
          {actionError && <p role="alert" className="mt-3 text-sm text-red-600">{actionError}</p>}
          <div className="mt-5 flex justify-end gap-2">
            <Button type="button" variant="secondary" disabled={isMutating} onClick={() => setPendingAction(null)}>
              {actionConflicted ? 'Close and review latest account' : 'Cancel'}
            </Button>
            <Button type="submit" variant={pendingAction?.action === 'deactivate' ? 'danger' : 'primary'}
              isLoading={isMutating} disabled={!reason.trim() || actionConflicted}>Confirm</Button>
          </div>
        </form>
      </Dialog>
    </div>
  )
}
